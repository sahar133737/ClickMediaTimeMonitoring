using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;

namespace ClickMediaWorkTime.Services
{
    internal static class ProjectService
    {
        public static DataTable GetAll()
        {
            return Db.ExecuteDataTable(@"
SELECT p.Id, p.Name, p.ClientName, p.StatusId, s.Name AS StatusName, p.StartDate, p.EndDate, p.PlannedHours
FROM dbo.Projects p
INNER JOIN dbo.ProjectStatuses s ON s.Id = p.StatusId
WHERE p.IsDeleted = 0
ORDER BY p.Name;");
        }

        public static DataTable GetStatuses()
        {
            return Db.ExecuteDataTable("SELECT Id, Name FROM dbo.ProjectStatuses ORDER BY Name;");
        }

        public static DataTable GetLookupActive()
        {
            return Db.ExecuteDataTable(@"
SELECT p.Id, p.Name + ISNULL(N' — ' + NULLIF(LTRIM(RTRIM(p.ClientName)), N''), N'') AS DisplayName
FROM dbo.Projects p
WHERE p.IsDeleted = 0
ORDER BY p.Name;");
        }

        public static void Insert(string name, string client, int statusId, System.DateTime start, System.DateTime? end, decimal plannedHours)
        {
            Db.ExecuteNonQuery(
                @"INSERT INTO dbo.Projects (Name, ClientName, StatusId, StartDate, EndDate, PlannedHours)
                  VALUES (@Name, @Client, @Status, @Start, @End, @Hours);",
                new SqlParameter("@Name", name),
                new SqlParameter("@Client", (object)client ?? System.DBNull.Value),
                new SqlParameter("@Status", statusId),
                new SqlParameter("@Start", start.Date),
                new SqlParameter("@End", (object)end?.Date ?? System.DBNull.Value),
                new SqlParameter("@Hours", plannedHours));
        }

        public static void Update(int id, string name, string client, int statusId, System.DateTime start, System.DateTime? end, decimal plannedHours)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.Projects
                  SET Name = @Name, ClientName = @Client, StatusId = @Status, StartDate = @Start, EndDate = @End, PlannedHours = @Hours
                  WHERE Id = @Id AND IsDeleted = 0;",
                new SqlParameter("@Id", id),
                new SqlParameter("@Name", name),
                new SqlParameter("@Client", (object)client ?? System.DBNull.Value),
                new SqlParameter("@Status", statusId),
                new SqlParameter("@Start", start.Date),
                new SqlParameter("@End", (object)end?.Date ?? System.DBNull.Value),
                new SqlParameter("@Hours", plannedHours));
        }

        public static void SoftDelete(int id)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.Projects SET IsDeleted = 1 WHERE Id = @Id;",
                new SqlParameter("@Id", id));
        }
    }
}
