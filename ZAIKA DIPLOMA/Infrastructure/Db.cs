using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ClickMediaWorkTime.Infrastructure
{
    internal static class Db
    {
        public static string MasterConnectionString =>
            ConnectionStringHelper.GetRequired("MasterConnection");

        public static string AppConnectionString =>
            ConnectionStringHelper.GetRequired("AppConnection");

        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(AppConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                connection.Open();
                return command.ExecuteScalar();
            }
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(AppConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(AppConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                using (var adapter = new SqlDataAdapter(command))
                {
                    var table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        public static int ExecuteNonQueryMaster(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(MasterConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }
    }
}
