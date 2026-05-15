using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class BackupService
    {
        private const string DatabaseName = "ClickMediaTimeDB";

        public static string GetRecommendedBackupDirectory()
        {
            try
            {
                var t = Db.ExecuteDataTable("SELECT CONVERT(NVARCHAR(500), SERVERPROPERTY('InstanceDefaultBackupPath')) AS p;");
                if (t.Rows.Count > 0)
                {
                    var p = t.Rows[0]["p"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        return p.TrimEnd('\\');
                    }
                }
            }
            catch
            {
                // ignore
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ClickMediaWorkTime",
                "Backups");
        }

        public static string GetDefaultBackupFilePath(string fileName)
        {
            return Path.Combine(GetRecommendedBackupDirectory(), fileName);
        }

        public static void CreateBackupToFile(string backupFilePath, string comment, bool isAuto)
        {
            var directory = Path.GetDirectoryName(backupFilePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("Не удалось определить каталог для файла резервной копии.");
            }

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Не удалось создать каталог. Укажите путь, доступный службе SQL Server.",
                    ex);
            }

            var sql = $"BACKUP DATABASE [{DatabaseName}] TO DISK = @Path WITH INIT, COMPRESSION;";
            try
            {
                Db.ExecuteNonQueryMaster(sql, new SqlParameter("@Path", backupFilePath));
            }
            catch (SqlException ex)
            {
                if (ex.Message.IndexOf("Operating system error 5", StringComparison.OrdinalIgnoreCase) >= 0
                    || ex.Message.IndexOf("Ошибка операционной системы 5", StringComparison.OrdinalIgnoreCase) >= 0
                    || ex.Number == 3201)
                {
                    throw new InvalidOperationException(
                        "Служба SQL Server не имеет прав на запись в выбранную папку. Сохраните .bak в: "
                        + GetRecommendedBackupDirectory(),
                        ex);
                }

                throw;
            }

            var fileInfo = new FileInfo(backupFilePath);
            Db.ExecuteNonQuery(
                @"INSERT INTO dbo.Backups (FileName, FilePath, SizeBytes, CreatedByUserId, Comment, IsAuto)
                  VALUES (@FileName, @FilePath, @SizeBytes, @UserId, @Comment, @IsAuto);",
                new SqlParameter("@FileName", Path.GetFileName(backupFilePath)),
                new SqlParameter("@FilePath", backupFilePath),
                new SqlParameter("@SizeBytes", fileInfo.Exists ? fileInfo.Length : 0),
                new SqlParameter("@UserId", CurrentUserContext.UserId == 0 ? (object)DBNull.Value : CurrentUserContext.UserId),
                new SqlParameter("@Comment", (object)comment ?? DBNull.Value),
                new SqlParameter("@IsAuto", isAuto));
        }

        public static DataTable GetBackups()
        {
            return GetFiltered(string.Empty, 0);
        }

        /// <param name="kindIndex">0 — все, 1 — только автоматические, 2 — только ручные.</param>
        public static DataTable GetFiltered(string search, int kindIndex)
        {
            var s = (search ?? string.Empty).Trim();
            if (s.Length > 300)
            {
                s = s.Substring(0, 300);
            }

            const string sql = @"
SELECT TOP 500 Id, FileName, FilePath, SizeBytes, CreationDate, Comment, IsAuto
FROM dbo.Backups
WHERE (
    @Kind = 0
    OR (@Kind = 1 AND IsAuto = 1)
    OR (@Kind = 2 AND IsAuto = 0)
)
AND (
    @Search = N''
    OR FileName LIKE N'%' + @Search + N'%'
    OR FilePath LIKE N'%' + @Search + N'%'
    OR ISNULL(Comment, N'') LIKE N'%' + @Search + N'%'
)
ORDER BY CreationDate DESC;";
            return Db.ExecuteDataTable(
                sql,
                new SqlParameter("@Search", s),
                new SqlParameter("@Kind", kindIndex));
        }

        public static void RestoreBackup(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Файл резервной копии не найден.", backupFilePath);
            }

            using (var connection = new SqlConnection(Db.MasterConnectionString))
            {
                connection.Open();
                var sql = $@"
ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{DatabaseName}] FROM DISK = @Path WITH REPLACE;
ALTER DATABASE [{DatabaseName}] SET MULTI_USER;";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 600;
                    command.Parameters.AddWithValue("@Path", backupFilePath);
                    command.ExecuteNonQuery();
                }
            }

            AuditService.LogChange("Backups", "RESTORE", null, null, "{\"FilePath\":\"" + backupFilePath.Replace("\\", "\\\\") + "\"}");
        }
    }
}
