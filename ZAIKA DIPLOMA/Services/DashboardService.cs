using System;
using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class DashboardService
    {
        public static int GetActiveEmployeesCount()
        {
            var obj = Db.ExecuteScalar("SELECT COUNT(*) FROM dbo.Employees WHERE IsDeleted = 0 AND IsActive = 1;");
            return Convert.ToInt32(obj);
        }

        public static int GetActiveProjectsCount()
        {
            const string sql = @"
SELECT COUNT(*) FROM dbo.Projects p
INNER JOIN dbo.ProjectStatuses s ON s.Id = p.StatusId
WHERE p.IsDeleted = 0 AND s.Name <> N'Завершён';";
            return Convert.ToInt32(Db.ExecuteScalar(sql));
        }

        public static decimal GetHoursSumLast30Days()
        {
            const string sql = @"
SELECT ISNULL(SUM(
    DATEDIFF(MINUTE, CAST(w.WorkDate AS datetime) + CAST(w.StartTime AS datetime),
              CAST(w.WorkDate AS datetime) + CAST(w.EndTime AS datetime)) - w.BreakMinutes
) / 60.0, 0)
FROM dbo.WorkTimeEntries w
WHERE w.WorkDate >= DATEADD(DAY, -30, CAST(GETDATE() AS date));";
            return Convert.ToDecimal(Db.ExecuteScalar(sql));
        }

        public static DataTable GetHoursByDayLast14Days()
        {
            const string sql = @"
DECLARE @to DATE = CAST(GETDATE() AS date);
DECLARE @from DATE = DATEADD(DAY, -13, @to);

;WITH d AS (
    SELECT @from AS d
    UNION ALL
    SELECT DATEADD(DAY, 1, d) FROM d WHERE d < @to
)
SELECT d.d AS WorkDay,
       CAST(ISNULL(SUM(
            (DATEDIFF(MINUTE,
                CAST(w.WorkDate AS datetime) + CAST(w.StartTime AS datetime),
                CAST(w.WorkDate AS datetime) + CAST(w.EndTime AS datetime)) - w.BreakMinutes) / 60.0), 0) AS DECIMAL(12,2)) AS HoursTotal
FROM d
LEFT JOIN dbo.WorkTimeEntries w ON w.WorkDate = d.d
GROUP BY d.d
ORDER BY d.d OPTION (MAXRECURSION 366);";
            return Db.ExecuteDataTable(sql);
        }

        public static DataTable GetRecentTimeEntries(int top)
        {
            var safeTop = Math.Max(1, Math.Min(top, 500));
            var sql = $@"
SELECT TOP ({safeTop})
    w.Id,
    e.FullName AS EmployeeName,
    p.Name AS ProjectName,
    w.WorkDate,
    w.StartTime,
    w.EndTime,
    t.Name AS TypeName,
    DATEDIFF(MINUTE,
        CAST(w.WorkDate AS datetime) + CAST(w.StartTime AS datetime),
        CAST(w.WorkDate AS datetime) + CAST(w.EndTime AS datetime)) - w.BreakMinutes AS NetMinutes
FROM dbo.WorkTimeEntries w
INNER JOIN dbo.Employees e ON e.Id = w.EmployeeId
INNER JOIN dbo.Projects p ON p.Id = w.ProjectId
INNER JOIN dbo.TimeEntryTypes t ON t.Id = w.TypeId
ORDER BY w.WorkDate DESC, w.Id DESC;";

            if (string.Equals(CurrentUserContext.RoleCode, "EMPLOYEE", StringComparison.OrdinalIgnoreCase)
                && CurrentUserContext.EmployeeId.HasValue)
            {
                sql = $@"
SELECT TOP ({safeTop})
    w.Id,
    e.FullName AS EmployeeName,
    p.Name AS ProjectName,
    w.WorkDate,
    w.StartTime,
    w.EndTime,
    t.Name AS TypeName,
    DATEDIFF(MINUTE,
        CAST(w.WorkDate AS datetime) + CAST(w.StartTime AS datetime),
        CAST(w.WorkDate AS datetime) + CAST(w.EndTime AS datetime)) - w.BreakMinutes AS NetMinutes
FROM dbo.WorkTimeEntries w
INNER JOIN dbo.Employees e ON e.Id = w.EmployeeId
INNER JOIN dbo.Projects p ON p.Id = w.ProjectId
INNER JOIN dbo.TimeEntryTypes t ON t.Id = w.TypeId
WHERE w.EmployeeId = @Emp
ORDER BY w.WorkDate DESC, w.Id DESC;";
                return Db.ExecuteDataTable(sql, new SqlParameter("@Emp", CurrentUserContext.EmployeeId.Value));
            }

            return Db.ExecuteDataTable(sql);
        }
    }
}
