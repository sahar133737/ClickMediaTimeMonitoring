using System;
using System.Configuration;
using System.Data.SqlClient;

namespace ClickMediaWorkTime.Infrastructure
{
    /// <summary>
    /// Нормализация строк подключения для запуска на разных ПК (TLS / сертификат SQL Server).
    /// </summary>
    internal static class ConnectionStringHelper
    {
        public static string GetRequired(string configName)
        {
            var settings = ConfigurationManager.ConnectionStrings[configName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "В App.config не задана строка подключения \"" + configName + "\".");
            }

            return Normalize(settings.ConnectionString);
        }

        /// <summary>
        /// Добавляет параметры, устраняющие ошибку «сертификат не доверен» на SQL Server 2019+.
        /// </summary>
        public static string Normalize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Пустая строка подключения.", nameof(connectionString));
            }

            var result = connectionString.Trim().TrimEnd(';');

            if (IndexOfKey(result, "TrustServerCertificate") < 0)
            {
                result += ";TrustServerCertificate=True";
            }

            if (IndexOfKey(result, "Encrypt") < 0)
            {
                result += ";Encrypt=False";
            }

            return result;
        }

        public static string DescribeServer(string connectionString)
        {
            try
            {
                var b = new SqlConnectionStringBuilder(Normalize(connectionString));
                return string.IsNullOrWhiteSpace(b.DataSource) ? "(не указан)" : b.DataSource;
            }
            catch
            {
                return "(не удалось разобрать)";
            }
        }

        public static void TestOpen(string connectionString)
        {
            using (var connection = new SqlConnection(Normalize(connectionString)))
            {
                connection.Open();
            }
        }

        private static int IndexOfKey(string connectionString, string key)
        {
            return connectionString.IndexOf(key + "=", StringComparison.OrdinalIgnoreCase);
        }
    }
}
