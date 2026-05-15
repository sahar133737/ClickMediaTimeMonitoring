using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;

namespace ClickMediaWorkTime.Services
{
    internal static class EmployeeService
    {
        public static DataTable GetAll()
        {
            return GetFiltered(string.Empty, 0);
        }

        /// <param name="activeMode">0 — все, 1 — активные, 2 — неактивные.</param>
        public static DataTable GetFiltered(string search, int activeMode)
        {
            var s = (search ?? string.Empty).Trim();
            if (s.Length > 200)
            {
                s = s.Substring(0, 200);
            }

            return Db.ExecuteDataTable(@"
SELECT e.Id,
       e.FullName,
       e.PersonnelNumber,
       e.Phone,
       e.Email,
       e.HireDate,
       e.IsActive,
       d.Name AS DepartmentName,
       p.Name AS PositionName
FROM dbo.Employees e
INNER JOIN dbo.Departments d ON d.Id = e.DepartmentId
INNER JOIN dbo.Positions p ON p.Id = e.PositionId
WHERE e.IsDeleted = 0
AND (
    @ActiveMode = 0
    OR (@ActiveMode = 1 AND e.IsActive = 1)
    OR (@ActiveMode = 2 AND e.IsActive = 0)
)
AND (
    @Search = N''
    OR e.FullName LIKE N'%' + @Search + N'%'
    OR e.PersonnelNumber LIKE N'%' + @Search + N'%'
    OR ISNULL(e.Phone, N'') LIKE N'%' + @Search + N'%'
    OR ISNULL(e.Email, N'') LIKE N'%' + @Search + N'%'
    OR d.Name LIKE N'%' + @Search + N'%'
    OR p.Name LIKE N'%' + @Search + N'%'
)
ORDER BY e.FullName;",
                new SqlParameter("@Search", s),
                new SqlParameter("@ActiveMode", activeMode));
        }

        public static DataTable GetLookupActive()
        {
            return Db.ExecuteDataTable(@"
SELECT Id, FullName + N' (' + PersonnelNumber + N')' AS DisplayName
FROM dbo.Employees
WHERE IsDeleted = 0 AND IsActive = 1
ORDER BY FullName;");
        }

        public static void Insert(int departmentId, int positionId, string fullName, string personnelNumber, string phone, string email, System.DateTime hireDate, bool isActive)
        {
            Db.ExecuteNonQuery(
                @"INSERT INTO dbo.Employees (DepartmentId, PositionId, FullName, PersonnelNumber, Phone, Email, HireDate, IsActive)
                  VALUES (@Dept, @Pos, @Name, @Num, @Phone, @Email, @Hire, @Active);",
                new SqlParameter("@Dept", departmentId),
                new SqlParameter("@Pos", positionId),
                new SqlParameter("@Name", fullName),
                new SqlParameter("@Num", personnelNumber),
                new SqlParameter("@Phone", (object)phone ?? System.DBNull.Value),
                new SqlParameter("@Email", (object)email ?? System.DBNull.Value),
                new SqlParameter("@Hire", hireDate.Date),
                new SqlParameter("@Active", isActive));
        }

        public static void Update(int id, int departmentId, int positionId, string fullName, string personnelNumber, string phone, string email, System.DateTime hireDate, bool isActive)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.Employees
                  SET DepartmentId = @Dept, PositionId = @Pos, FullName = @Name, PersonnelNumber = @Num,
                      Phone = @Phone, Email = @Email, HireDate = @Hire, IsActive = @Active
                  WHERE Id = @Id AND IsDeleted = 0;",
                new SqlParameter("@Id", id),
                new SqlParameter("@Dept", departmentId),
                new SqlParameter("@Pos", positionId),
                new SqlParameter("@Name", fullName),
                new SqlParameter("@Num", personnelNumber),
                new SqlParameter("@Phone", (object)phone ?? System.DBNull.Value),
                new SqlParameter("@Email", (object)email ?? System.DBNull.Value),
                new SqlParameter("@Hire", hireDate.Date),
                new SqlParameter("@Active", isActive));
        }

        public static void SoftDelete(int id)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.Employees SET IsDeleted = 1 WHERE Id = @Id;",
                new SqlParameter("@Id", id));
        }
    }
}
