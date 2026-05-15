using System;

namespace ClickMediaWorkTime.Security
{
    internal static class CurrentUserContext
    {
        public static int UserId { get; set; }
        public static int RoleId { get; set; }
        public static string RoleCode { get; set; }
        /// <summary>Название роли из справочника (на русском).</summary>
        public static string RoleName { get; set; }

        public static string FullName { get; set; }
        public static string LoginName { get; set; }
        public static int? EmployeeId { get; set; }
        public static string IpAddress { get; set; }

        /// <summary>Роль для отображения в интерфейсе (русский текст).</summary>
        public static string RoleDisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(RoleName))
                {
                    return RoleName.Trim();
                }

                if (string.IsNullOrWhiteSpace(RoleCode))
                {
                    return "—";
                }

                if (RoleCode.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    return "Администратор";
                }

                if (RoleCode.Equals("MANAGER", StringComparison.OrdinalIgnoreCase))
                {
                    return "Руководитель";
                }

                if (RoleCode.Equals("EMPLOYEE", StringComparison.OrdinalIgnoreCase))
                {
                    return "Сотрудник";
                }

                return RoleCode;
            }
        }
    }
}
