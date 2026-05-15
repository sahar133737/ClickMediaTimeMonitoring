using System;
using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class WorkTimeEntryService
    {
        public static DataTable GetTypes()
        {
            return Db.ExecuteDataTable("SELECT Id, Name FROM dbo.TimeEntryTypes ORDER BY Name;");
        }

        public static int InsertType(string name)
        {
            var n = (name ?? string.Empty).Trim();
            if (n.Length < 2 || n.Length > 120)
            {
                throw new ArgumentException("Название типа: от 2 до 120 символов.");
            }

            var idObj = Db.ExecuteScalar(
                @"INSERT INTO dbo.TimeEntryTypes (Name) OUTPUT INSERTED.Id VALUES (@N);",
                new SqlParameter("@N", n));
            var id = Convert.ToInt32(idObj);
            AuditService.LogChange("TimeEntryTypes", "INSERT", id.ToString(), null, n);
            return id;
        }

        public static DataTable GetAll()
        {
            const string baseSql = @"
SELECT w.Id,
       p.Id AS ProjectId,
       e.FullName AS EmployeeName,
       p.Name AS ProjectName,
       w.WorkDate,
       w.StartTime,
       w.EndTime,
       w.BreakMinutes,
       t.Name AS TypeName,
       w.Comment,
       DATEDIFF(MINUTE,
           CAST(w.WorkDate AS datetime) + CAST(w.StartTime AS datetime),
           CAST(w.WorkDate AS datetime) + CAST(w.EndTime AS datetime)) - w.BreakMinutes AS NetMinutes
FROM dbo.WorkTimeEntries w
INNER JOIN dbo.Employees e ON e.Id = w.EmployeeId
INNER JOIN dbo.Projects p ON p.Id = w.ProjectId
INNER JOIN dbo.TimeEntryTypes t ON t.Id = w.TypeId";

            if (string.Equals(CurrentUserContext.RoleCode, "EMPLOYEE", StringComparison.OrdinalIgnoreCase)
                && CurrentUserContext.EmployeeId.HasValue)
            {
                return Db.ExecuteDataTable(
                    baseSql + " WHERE w.EmployeeId = @Emp ORDER BY w.WorkDate DESC, w.Id DESC;",
                    new SqlParameter("@Emp", CurrentUserContext.EmployeeId.Value));
            }

            return Db.ExecuteDataTable(baseSql + " ORDER BY w.WorkDate DESC, w.Id DESC;");
        }

        public static void Insert(int employeeId, int projectId, DateTime workDate, TimeSpan start, TimeSpan end, int breakMinutes, int typeId, string comment)
        {
            Validate(employeeId, workDate, start, end, breakMinutes);
            EnforceEmployeeScope(employeeId);

            Db.ExecuteNonQuery(
                @"INSERT INTO dbo.WorkTimeEntries
                    (EmployeeId, ProjectId, WorkDate, StartTime, EndTime, BreakMinutes, TypeId, Comment, CreatedByUserId)
                  VALUES (@Emp, @Proj, @Date, @Start, @End, @Break, @Type, @Comment, @User);",
                new SqlParameter("@Emp", employeeId),
                new SqlParameter("@Proj", projectId),
                new SqlParameter("@Date", workDate.Date),
                new SqlParameter("@Start", start),
                new SqlParameter("@End", end),
                new SqlParameter("@Break", breakMinutes),
                new SqlParameter("@Type", typeId),
                new SqlParameter("@Comment", (object)comment ?? DBNull.Value),
                new SqlParameter("@User", CurrentUserContext.UserId == 0 ? (object)DBNull.Value : CurrentUserContext.UserId));
        }

        public static void Update(long id, int employeeId, int projectId, DateTime workDate, TimeSpan start, TimeSpan end, int breakMinutes, int typeId, string comment)
        {
            Validate(employeeId, workDate, start, end, breakMinutes);
            if (string.Equals(CurrentUserContext.RoleCode, "EMPLOYEE", StringComparison.OrdinalIgnoreCase)
                && CurrentUserContext.EmployeeId.HasValue)
            {
                var owner = Db.ExecuteScalar(
                    "SELECT EmployeeId FROM dbo.WorkTimeEntries WHERE Id = @Id;",
                    new SqlParameter("@Id", id));
                if (owner == null || owner == DBNull.Value || Convert.ToInt32(owner) != CurrentUserContext.EmployeeId.Value)
                {
                    throw new InvalidOperationException("Недостаточно прав для изменения этой записи.");
                }
            }

            EnforceEmployeeScope(employeeId);

            Db.ExecuteNonQuery(
                @"UPDATE dbo.WorkTimeEntries
                  SET EmployeeId = @Emp, ProjectId = @Proj, WorkDate = @Date, StartTime = @Start, EndTime = @End,
                      BreakMinutes = @Break, TypeId = @Type, Comment = @Comment
                  WHERE Id = @Id;",
                new SqlParameter("@Id", id),
                new SqlParameter("@Emp", employeeId),
                new SqlParameter("@Proj", projectId),
                new SqlParameter("@Date", workDate.Date),
                new SqlParameter("@Start", start),
                new SqlParameter("@End", end),
                new SqlParameter("@Break", breakMinutes),
                new SqlParameter("@Type", typeId),
                new SqlParameter("@Comment", (object)comment ?? DBNull.Value));
        }

        public static void Delete(long id)
        {
            if (string.Equals(CurrentUserContext.RoleCode, "EMPLOYEE", StringComparison.OrdinalIgnoreCase)
                && CurrentUserContext.EmployeeId.HasValue)
            {
                var owner = Db.ExecuteScalar(
                    "SELECT EmployeeId FROM dbo.WorkTimeEntries WHERE Id = @Id;",
                    new SqlParameter("@Id", id));
                if (owner == null || owner == DBNull.Value || Convert.ToInt32(owner) != CurrentUserContext.EmployeeId.Value)
                {
                    throw new InvalidOperationException("Недостаточно прав для удаления этой записи.");
                }
            }

            Db.ExecuteNonQuery(
                "DELETE FROM dbo.WorkTimeEntries WHERE Id = @Id;",
                new SqlParameter("@Id", id));
        }

        private static void Validate(int employeeId, DateTime workDate, TimeSpan start, TimeSpan end, int breakMinutes)
        {
            if (employeeId <= 0)
            {
                throw new ArgumentException("Выберите сотрудника.");
            }

            if (end <= start)
            {
                throw new InvalidOperationException("Время окончания должно быть позже времени начала.");
            }

            var net = (int)(end - start).TotalMinutes - breakMinutes;
            if (breakMinutes < 0 || breakMinutes >= 24 * 60)
            {
                throw new InvalidOperationException("Перерыв должен быть в диапазоне 0..1439 минут.");
            }

            if (net <= 0)
            {
                throw new InvalidOperationException("Чистое рабочее время после перерыва должно быть больше нуля.");
            }

            if (workDate.Date > DateTime.Today.AddDays(1))
            {
                throw new InvalidOperationException("Дата работы не может быть из далёкого будущего.");
            }
        }

        private static void EnforceEmployeeScope(int employeeId)
        {
            if (string.Equals(CurrentUserContext.RoleCode, "EMPLOYEE", StringComparison.OrdinalIgnoreCase))
            {
                if (!CurrentUserContext.EmployeeId.HasValue || employeeId != CurrentUserContext.EmployeeId.Value)
                {
                    throw new InvalidOperationException("Сотрудник может вести учёт только за себя.");
                }
            }
        }
    }
}
