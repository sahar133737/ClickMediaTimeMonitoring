using System;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class ErrorLogService
    {
        public static void LogUiException(string source, Exception ex)
        {
            try
            {
                const string sql = @"
INSERT INTO dbo.ErrorLog (UserId, Source, Message, StackTrace)
VALUES (@UserId, @Src, @Msg, @Stack);";
                Db.ExecuteNonQuery(
                    sql,
                    new SqlParameter("@UserId", CurrentUserContext.UserId == 0 ? (object)DBNull.Value : CurrentUserContext.UserId),
                    new SqlParameter("@Src", source ?? "UI"),
                    new SqlParameter("@Msg", ex?.Message ?? "Неизвестная ошибка"),
                    new SqlParameter("@Stack", (object)ex?.ToString() ?? DBNull.Value));
            }
            catch
            {
                // последний рубеж — игнорируем
            }
        }
    }
}
