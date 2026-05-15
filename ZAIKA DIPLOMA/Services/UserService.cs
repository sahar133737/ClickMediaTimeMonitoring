using System;
using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class UserService
    {
        public static DataTable GetAll()
        {
            return GetFiltered(string.Empty, 0);
        }

        /// <param name="activeMode">0 — все, 1 — активные, 2 — отключённые.</param>
        public static DataTable GetFiltered(string search, int activeMode)
        {
            var s = (search ?? string.Empty).Trim();
            if (s.Length > 200)
            {
                s = s.Substring(0, 200);
            }

            const string sql = @"
SELECT u.Id,
       u.LoginName,
       u.FullName,
       r.Name AS RoleName,
       u.RegistrationDate,
       u.IsActive,
       e.FullName AS LinkedEmployee
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.Id = u.RoleId
LEFT JOIN dbo.Employees e ON e.Id = u.EmployeeId
WHERE u.IsDeleted = 0
AND (
    @ActiveMode = 0
    OR (@ActiveMode = 1 AND u.IsActive = 1)
    OR (@ActiveMode = 2 AND u.IsActive = 0)
)
AND (
    @Search = N''
    OR u.LoginName LIKE N'%' + @Search + N'%'
    OR u.FullName LIKE N'%' + @Search + N'%'
    OR r.Name LIKE N'%' + @Search + N'%'
    OR ISNULL(e.FullName, N'') LIKE N'%' + @Search + N'%'
)
ORDER BY u.LoginName;";
            return Db.ExecuteDataTable(
                sql,
                new SqlParameter("@Search", s),
                new SqlParameter("@ActiveMode", activeMode));
        }

        public static DataTable GetRolesLookup()
        {
            return Db.ExecuteDataTable("SELECT Id, Name FROM dbo.Roles WHERE IsDeleted = 0 ORDER BY AccessLevel DESC;");
        }

        public static void Insert(string login, string password, string fullName, int roleId, int? employeeId, bool isActive)
        {
            var hash = PasswordHasher.HashPassword(password);
            Db.ExecuteNonQuery(
                @"INSERT INTO dbo.Users (LoginName, PasswordHash, FullName, RoleId, EmployeeId, IsActive, MustChangePassword)
                  VALUES (@Login, @Hash, @Name, @Role, @Emp, @Active, 0);",
                new SqlParameter("@Login", login),
                new SqlParameter("@Hash", hash),
                new SqlParameter("@Name", fullName),
                new SqlParameter("@Role", roleId),
                new SqlParameter("@Emp", (object)employeeId ?? DBNull.Value),
                new SqlParameter("@Active", isActive));
        }

        public static void Update(int id, string login, string fullName, int roleId, int? employeeId, bool isActive, string newPasswordOrNull)
        {
            if (!string.IsNullOrWhiteSpace(newPasswordOrNull))
            {
                var hash = PasswordHasher.HashPassword(newPasswordOrNull);
                Db.ExecuteNonQuery(
                    @"UPDATE dbo.Users
                      SET LoginName = @Login, FullName = @Name, RoleId = @Role, EmployeeId = @Emp,
                          IsActive = @Active, PasswordHash = @Hash, LastPasswordChange = SYSUTCDATETIME()
                      WHERE Id = @Id AND IsDeleted = 0;",
                    new SqlParameter("@Id", id),
                    new SqlParameter("@Login", login),
                    new SqlParameter("@Name", fullName),
                    new SqlParameter("@Role", roleId),
                    new SqlParameter("@Emp", (object)employeeId ?? DBNull.Value),
                    new SqlParameter("@Active", isActive),
                    new SqlParameter("@Hash", hash));
            }
            else
            {
                Db.ExecuteNonQuery(
                    @"UPDATE dbo.Users
                      SET LoginName = @Login, FullName = @Name, RoleId = @Role, EmployeeId = @Emp, IsActive = @Active
                      WHERE Id = @Id AND IsDeleted = 0;",
                    new SqlParameter("@Id", id),
                    new SqlParameter("@Login", login),
                    new SqlParameter("@Name", fullName),
                    new SqlParameter("@Role", roleId),
                    new SqlParameter("@Emp", (object)employeeId ?? DBNull.Value),
                    new SqlParameter("@Active", isActive));
            }
        }

        public static void SoftDelete(int id)
        {
            if (id == CurrentUserContext.UserId)
            {
                throw new InvalidOperationException("Нельзя удалить собственную учётную запись.");
            }

            Db.ExecuteNonQuery(
                @"UPDATE dbo.Users SET IsDeleted = 1 WHERE Id = @Id;",
                new SqlParameter("@Id", id));
        }
    }
}
