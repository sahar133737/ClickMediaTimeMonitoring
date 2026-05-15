using System;
using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class AuthService
    {
        public static bool TryLogin(string login, string password, string ipAddress, out string error)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                error = "Введите логин.";
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                error = "Введите пароль.";
                return false;
            }

            const string sql = @"
SELECT u.Id, u.RoleId, u.PasswordHash, u.FullName, u.IsActive, u.EmployeeId, r.Code AS RoleCode, r.Name AS RoleName
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.Id = u.RoleId
WHERE u.LoginName = @Login AND u.IsDeleted = 0;";

            DataTable table;
            try
            {
                table = Db.ExecuteDataTable(sql, new SqlParameter("@Login", login.Trim()));
            }
            catch (Exception ex)
            {
                error = "Ошибка подключения к БД: " + ex.Message;
                RegisterLoginAttempt(login, false, ipAddress, error);
                return false;
            }

            if (table.Rows.Count == 0)
            {
                error = "Пользователь не найден.";
                RegisterLoginAttempt(login, false, ipAddress, error);
                return false;
            }

            var row = table.Rows[0];
            if (!(bool)row["IsActive"])
            {
                error = "Учётная запись отключена.";
                RegisterLoginAttempt(login, false, ipAddress, error);
                return false;
            }

            var hash = row["PasswordHash"].ToString();
            if (!PasswordHasher.Verify(password, hash))
            {
                error = "Неверный пароль.";
                RegisterLoginAttempt(login, false, ipAddress, error);
                return false;
            }

            CurrentUserContext.UserId = Convert.ToInt32(row["Id"]);
            CurrentUserContext.RoleId = Convert.ToInt32(row["RoleId"]);
            CurrentUserContext.FullName = row["FullName"].ToString();
            CurrentUserContext.LoginName = login.Trim();
            CurrentUserContext.RoleCode = row["RoleCode"].ToString();
            CurrentUserContext.RoleName = row["RoleName"].ToString();
            CurrentUserContext.EmployeeId = row["EmployeeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["EmployeeId"]);
            CurrentUserContext.IpAddress = ipAddress;

            RegisterLoginAttempt(login, true, ipAddress, "Успешный вход");
            CreateSession();
            error = null;
            return true;
        }

        private static void RegisterLoginAttempt(string login, bool isSuccess, string ipAddress, string message)
        {
            try
            {
                const string sql = @"
INSERT INTO dbo.LoginAttempts (LoginName, IsSuccess, IPAddress, [Message])
VALUES (@Login, @Ok, @Ip, @Msg);";
                Db.ExecuteNonQuery(
                    sql,
                    new SqlParameter("@Login", login),
                    new SqlParameter("@Ok", isSuccess),
                    new SqlParameter("@Ip", (object)ipAddress ?? DBNull.Value),
                    new SqlParameter("@Msg", (object)message ?? DBNull.Value));
            }
            catch
            {
                // не блокируем вход из-за журнала
            }
        }

        private static void CreateSession()
        {
            try
            {
                const string sql = @"
INSERT INTO dbo.Sessions (UserId, IPAddress)
VALUES (@UserId, @Ip);";
                Db.ExecuteNonQuery(
                    sql,
                    new SqlParameter("@UserId", CurrentUserContext.UserId),
                    new SqlParameter("@Ip", (object)CurrentUserContext.IpAddress ?? DBNull.Value));
            }
            catch
            {
                // сессия — вспомогательная запись
            }
        }
    }
}
