using System;
using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class AuditService
    {
        public static DataTable GetAuditLog(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT TOP 2000 a.Id, a.[Timestamp], u.LoginName, a.TableName, a.OperationType, a.RecordId, a.IPAddress
FROM dbo.AuditLog a
LEFT JOIN dbo.Users u ON u.Id = a.UserId
WHERE a.[Timestamp] BETWEEN @From AND @To
ORDER BY a.[Timestamp] DESC;";
            return Db.ExecuteDataTable(
                sql,
                new SqlParameter("@From", from),
                new SqlParameter("@To", to));
        }

        public static void LogChange(string tableName, string operationType, string recordId, string oldJson, string newJson)
        {
            try
            {
                const string sql = @"
INSERT INTO dbo.AuditLog (UserId, TableName, OperationType, RecordId, OldValue, NewValue, IPAddress)
VALUES (@UserId, @Table, @Op, @RecId, @Old, @New, @Ip);";
                Db.ExecuteNonQuery(
                    sql,
                    new SqlParameter("@UserId", CurrentUserContext.UserId == 0 ? (object)DBNull.Value : CurrentUserContext.UserId),
                    new SqlParameter("@Table", tableName),
                    new SqlParameter("@Op", operationType),
                    new SqlParameter("@RecId", (object)recordId ?? DBNull.Value),
                    new SqlParameter("@Old", (object)oldJson ?? DBNull.Value),
                    new SqlParameter("@New", (object)newJson ?? DBNull.Value),
                    new SqlParameter("@Ip", (object)CurrentUserContext.IpAddress ?? DBNull.Value));
            }
            catch
            {
                // аудит не должен ронять бизнес-операцию
            }
        }
    }
}
