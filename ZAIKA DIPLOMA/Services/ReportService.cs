using System;
using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;

namespace ClickMediaWorkTime.Services
{
    internal static class ReportService
    {
        private const string MinutesExpr = @"DATEDIFF(MINUTE,
            CAST(w.WorkDate AS datetime) + CAST(w.StartTime AS datetime),
            CAST(w.WorkDate AS datetime) + CAST(w.EndTime AS datetime)) - w.BreakMinutes";

        /// <summary>Сводка «Пульс команды»: подразделение → сотрудник, часы и строка «ИТОГО».</summary>
        public static DataTable GetReportDepartmentEmployeeHours(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT d.Name AS DepartmentName,
       e.FullName AS EmployeeName,
       SUM(" + MinutesExpr + @") / 60.0 AS HoursTotal
FROM dbo.WorkTimeEntries w
INNER JOIN dbo.Employees e ON e.Id = w.EmployeeId AND e.IsDeleted = 0
INNER JOIN dbo.Departments d ON d.Id = e.DepartmentId AND d.IsDeleted = 0
WHERE w.WorkDate BETWEEN @From AND @To
GROUP BY d.Name, e.Id, e.FullName
ORDER BY d.Name, HoursTotal DESC;";
            var t = Db.ExecuteDataTable(
                sql,
                new SqlParameter("@From", from.Date),
                new SqlParameter("@To", to.Date));
            AppendTotalRow(t, "DepartmentName", "EmployeeName", "HoursTotal", "ИТОГО", string.Empty);
            return t;
        }

        /// <summary>«Маршрут проектов»: статус → проект, сумма часов и «ИТОГО».</summary>
        public static DataTable GetReportStatusProjectHours(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT s.Name AS StatusName,
       p.Name AS ProjectName,
       SUM(" + MinutesExpr + @") / 60.0 AS HoursTotal
FROM dbo.WorkTimeEntries w
INNER JOIN dbo.Projects p ON p.Id = w.ProjectId AND p.IsDeleted = 0
INNER JOIN dbo.ProjectStatuses s ON s.Id = p.StatusId
WHERE w.WorkDate BETWEEN @From AND @To
GROUP BY s.Name, s.Id, p.Id, p.Name
ORDER BY s.Name, HoursTotal DESC;";
            var t = Db.ExecuteDataTable(
                sql,
                new SqlParameter("@From", from.Date),
                new SqlParameter("@To", to.Date));
            AppendTotalRow(t, "StatusName", "ProjectName", "HoursTotal", "ИТОГО", string.Empty);
            return t;
        }

        private static void AppendTotalRow(DataTable t, string labelCol, string subCol, string sumCol, string label, string subLabel)
        {
            if (t == null || t.Rows.Count == 0 || !t.Columns.Contains(sumCol))
            {
                return;
            }

            decimal sum = 0;
            foreach (DataRow r in t.Rows)
            {
                if (r[sumCol] != DBNull.Value)
                {
                    sum += Convert.ToDecimal(r[sumCol]);
                }
            }

            var nr = t.NewRow();
            if (t.Columns.Contains(labelCol))
            {
                nr[labelCol] = label;
            }

            if (subLabel != null && t.Columns.Contains(subCol))
            {
                nr[subCol] = subLabel;
            }

            nr[sumCol] = sum;
            t.Rows.Add(nr);
        }
    }
}
