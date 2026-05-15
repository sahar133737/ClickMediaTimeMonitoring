using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;

namespace ClickMediaWorkTime.Services
{
    internal static class PositionService
    {
        public static DataTable GetAll()
        {
            return Db.ExecuteDataTable(@"
SELECT Id, Name, CreatedAt
FROM dbo.Positions
WHERE IsDeleted = 0
ORDER BY Name;");
        }

        public static void Insert(string name)
        {
            Db.ExecuteNonQuery(
                @"INSERT INTO dbo.Positions (Name) VALUES (@Name);",
                new SqlParameter("@Name", name));
        }

        public static void Update(int id, string name)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.Positions SET Name = @Name WHERE Id = @Id AND IsDeleted = 0;",
                new SqlParameter("@Id", id),
                new SqlParameter("@Name", name));
        }

        public static void SoftDelete(int id)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.Positions SET IsDeleted = 1 WHERE Id = @Id;",
                new SqlParameter("@Id", id));
        }
    }
}
