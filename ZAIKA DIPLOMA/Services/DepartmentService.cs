using System;
using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;

namespace ClickMediaWorkTime.Services
{
    internal static class DepartmentService
    {
        public static DataTable GetAll()
        {
            return Db.ExecuteDataTable(@"
SELECT Id, Name, Description, CreatedAt
FROM dbo.Departments
WHERE IsDeleted = 0
ORDER BY Name;");
        }

        public static void Insert(string name, string description)
        {
            Db.ExecuteNonQuery(
                @"INSERT INTO dbo.Departments (Name, Description) VALUES (@Name, @Desc);",
                new SqlParameter("@Name", name),
                new SqlParameter("@Desc", (object)description ?? DBNull.Value));
        }

        public static void Update(int id, string name, string description)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.Departments SET Name = @Name, Description = @Desc WHERE Id = @Id AND IsDeleted = 0;",
                new SqlParameter("@Id", id),
                new SqlParameter("@Name", name),
                new SqlParameter("@Desc", (object)description ?? DBNull.Value));
        }

        public static void SoftDelete(int id)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.Departments SET IsDeleted = 1 WHERE Id = @Id;",
                new SqlParameter("@Id", id));
        }
    }
}
