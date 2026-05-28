using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class DatabaseBootstrapper
    {
        public static void EnsureDatabase()
        {
            try
            {
                ConnectionStringHelper.TestOpen(Db.MasterConnectionString);
            }
            catch (Exception ex)
            {
                var server = ConnectionStringHelper.DescribeServer(Db.MasterConnectionString);
                throw new InvalidOperationException(
                    "Не удалось подключиться к SQL Server («" + server + "»)." + Environment.NewLine +
                    "Проверьте, что SQL Server или LocalDB установлен и запущен, а в файле " +
                    "ClickMediaWorkTime.exe.config (рядом с программой) указан правильный Server." +
                    Environment.NewLine + Environment.NewLine +
                    "Техническое описание: " + ex.Message,
                    ex);
            }

            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sql", "01_create_schema.sql");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("Не найден SQL-скрипт инициализации БД.", scriptPath);
            }

            var scriptText = File.ReadAllText(scriptPath);
            var batches = Regex.Split(scriptText, @"^\s*GO\s*$", RegexOptions.Multiline);

            using (var connection = new SqlConnection(Db.MasterConnectionString))
            {
                connection.Open();
                foreach (var rawBatch in batches)
                {
                    var batch = rawBatch.Trim();
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }

                    using (var command = new SqlCommand(batch, connection))
                    {
                        command.CommandTimeout = 120;
                        command.ExecuteNonQuery();
                    }
                }
            }

            SeedDefaultUsersIfNeeded();
        }

        private static void SeedDefaultUsersIfNeeded()
        {
            try
            {
                var countObj = Db.ExecuteScalar("SELECT COUNT(*) FROM dbo.Users WHERE IsDeleted = 0;");
                var count = Convert.ToInt32(countObj);
                if (count > 0)
                {
                    return;
                }

                var roles = Db.ExecuteDataTable("SELECT Id, Code FROM dbo.Roles WHERE IsDeleted = 0;");
                int adminRoleId = 0, managerRoleId = 0, employeeRoleId = 0;
                foreach (DataRow row in roles.Rows)
                {
                    var code = row["Code"].ToString();
                    var id = Convert.ToInt32(row["Id"]);
                    if (string.Equals(code, "ADMIN", StringComparison.OrdinalIgnoreCase))
                    {
                        adminRoleId = id;
                    }
                    else if (string.Equals(code, "MANAGER", StringComparison.OrdinalIgnoreCase))
                    {
                        managerRoleId = id;
                    }
                    else if (string.Equals(code, "EMPLOYEE", StringComparison.OrdinalIgnoreCase))
                    {
                        employeeRoleId = id;
                    }
                }

                var hash = PasswordHasher.HashPassword("Admin123!");

                Db.ExecuteNonQuery(
                    @"INSERT INTO dbo.Users (LoginName, PasswordHash, FullName, RoleId, EmployeeId, IsActive, MustChangePassword)
                      VALUES (@Login, @Hash, @Name, @RoleId, NULL, 1, 0);",
                    new SqlParameter("@Login", "admin"),
                    new SqlParameter("@Hash", hash),
                    new SqlParameter("@Name", "Системный администратор"),
                    new SqlParameter("@RoleId", adminRoleId));

                object mgrEmpId = Db.ExecuteScalar(
                    "SELECT TOP 1 Id FROM dbo.Employees WHERE PersonnelNumber = N'КМ-0001' AND IsDeleted = 0;");
                if (mgrEmpId != null && mgrEmpId != DBNull.Value && managerRoleId > 0)
                {
                    Db.ExecuteNonQuery(
                        @"INSERT INTO dbo.Users (LoginName, PasswordHash, FullName, RoleId, EmployeeId, IsActive, MustChangePassword)
                          VALUES (@Login, @Hash, @Name, @RoleId, @EmpId, 1, 0);",
                        new SqlParameter("@Login", "manager"),
                        new SqlParameter("@Hash", hash),
                        new SqlParameter("@Name", "Руководитель (демо)"),
                        new SqlParameter("@RoleId", managerRoleId),
                        new SqlParameter("@EmpId", Convert.ToInt32(mgrEmpId)));
                }

                object empEmpId = Db.ExecuteScalar(
                    "SELECT TOP 1 Id FROM dbo.Employees WHERE PersonnelNumber = N'КМ-0003' AND IsDeleted = 0;");
                if (empEmpId != null && empEmpId != DBNull.Value && employeeRoleId > 0)
                {
                    Db.ExecuteNonQuery(
                        @"INSERT INTO dbo.Users (LoginName, PasswordHash, FullName, RoleId, EmployeeId, IsActive, MustChangePassword)
                          VALUES (@Login, @Hash, @Name, @RoleId, @EmpId, 1, 0);",
                        new SqlParameter("@Login", "employee"),
                        new SqlParameter("@Hash", hash),
                        new SqlParameter("@Name", "Сотрудник (демо)"),
                        new SqlParameter("@RoleId", employeeRoleId),
                        new SqlParameter("@EmpId", Convert.ToInt32(empEmpId)));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "База создана, но не удалось создать учётные записи по умолчанию. Проверьте сиды сотрудников в SQL.",
                    ex);
            }
        }
    }
}
