using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;

namespace ClickMediaWorkTime.Services
{
    internal static class RolePermissionService
    {
        private static readonly HashSet<string> Cache = new HashSet<string>();

        public static void LoadCurrentRolePermissions()
        {
            Cache.Clear();
            const string sql = @"
SELECT PermissionKey
FROM dbo.RolePermissions
WHERE RoleId = @RoleId AND IsAllowed = 1;";
            var table = Db.ExecuteDataTable(sql, new SqlParameter("@RoleId", CurrentUserContext.RoleId));
            foreach (DataRow row in table.Rows)
            {
                Cache.Add(row["PermissionKey"].ToString());
            }
        }

        public static bool HasPermission(string permissionKey)
        {
            return Cache.Contains(permissionKey);
        }

        public static DataTable GetRoles()
        {
            return Db.ExecuteDataTable("SELECT Id, Name, Code, AccessLevel FROM dbo.Roles WHERE IsDeleted = 0 ORDER BY AccessLevel DESC;");
        }

        public static DataTable GetPermissionsByRole(int roleId)
        {
            const string sql = @"
WITH AllKeys AS (
    SELECT N'module.dashboard' AS PermissionKey, 5 AS SortOrder UNION ALL
    SELECT N'module.departments', 10 UNION ALL
    SELECT N'module.positions', 20 UNION ALL
    SELECT N'module.employees', 30 UNION ALL
    SELECT N'module.projects', 40 UNION ALL
    SELECT N'module.worktime', 50 UNION ALL
    SELECT N'module.users', 60 UNION ALL
    SELECT N'module.reports', 70 UNION ALL
    SELECT N'module.backups', 80 UNION ALL
    SELECT N'module.audit', 90 UNION ALL
    SELECT N'module.admin', 100
)
SELECT k.PermissionKey,
       CAST(ISNULL(rp.IsAllowed, 0) AS bit) AS IsAllowed
FROM AllKeys k
LEFT JOIN dbo.RolePermissions rp
    ON rp.RoleId = @RoleId AND rp.PermissionKey = k.PermissionKey
ORDER BY k.SortOrder;";
            return Db.ExecuteDataTable(sql, new SqlParameter("@RoleId", roleId));
        }

        public static void SavePermission(int roleId, string permissionKey, bool isAllowed)
        {
            const string sql = @"
MERGE dbo.RolePermissions AS t
USING (SELECT @RoleId AS RoleId, @Key AS PermissionKey) AS s
ON t.RoleId = s.RoleId AND t.PermissionKey = s.PermissionKey
WHEN MATCHED THEN UPDATE SET IsAllowed = @Allowed
WHEN NOT MATCHED THEN INSERT (RoleId, PermissionKey, IsAllowed) VALUES (@RoleId, @Key, @Allowed);";
            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@RoleId", roleId),
                new SqlParameter("@Key", permissionKey),
                new SqlParameter("@Allowed", isAllowed));
        }
    }
}
