using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ClickMediaWorkTime.UI
{
    internal static class GridHeaderMap
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Maps =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "departments",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Name", "Подразделение" },
                        { "Description", "Описание" },
                        { "CreatedAt", "Создано" }
                    }
                },
                {
                    "positions",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Name", "Должность" },
                        { "CreatedAt", "Создано" }
                    }
                },
                {
                    "employees",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "FullName", "ФИО" },
                        { "PersonnelNumber", "Табельный номер" },
                        { "Phone", "Телефон" },
                        { "Email", "E-mail" },
                        { "HireDate", "Дата приёма" },
                        { "IsActive", "Активен" },
                        { "DepartmentName", "Подразделение" },
                        { "PositionName", "Должность" }
                    }
                },
                {
                    "projects",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Name", "Проект" },
                        { "ClientName", "Клиент" },
                        { "StatusName", "Статус" },
                        { "StartDate", "Начало" },
                        { "EndDate", "Окончание" },
                        { "PlannedHours", "План, часы" }
                    }
                },
                {
                    "worktime",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "EmployeeName", "Сотрудник" },
                        { "ProjectName", "Проект" },
                        { "WorkDate", "Дата" },
                        { "StartTime", "Начало" },
                        { "EndTime", "Окончание" },
                        { "BreakMinutes", "Перерыв, мин" },
                        { "TypeName", "Тип учёта" },
                        { "Comment", "Комментарий" },
                        { "NetMinutes", "Чистое время, мин" }
                    }
                },
                {
                    "users",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "LoginName", "Логин" },
                        { "FullName", "ФИО" },
                        { "RoleName", "Роль" },
                        { "RegistrationDate", "Регистрация" },
                        { "IsActive", "Активен" },
                        { "LinkedEmployee", "Сотрудник" }
                    }
                },
                {
                    "backups",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "FileName", "Файл" },
                        { "FilePath", "Путь" },
                        { "SizeBytes", "Размер, байт" },
                        { "CreationDate", "Дата создания" },
                        { "Comment", "Комментарий" },
                        { "IsAuto", "Автоматически" }
                    }
                },
                {
                    "audit",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "LoginName", "Пользователь" },
                        { "TableName", "Таблица" },
                        { "OperationType", "Операция" },
                        { "Timestamp", "Время" },
                        { "IPAddress", "IP-адрес" }
                    }
                },
                {
                    "reportsEmp",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "EmployeeName", "Сотрудник" },
                        { "HoursTotal", "Часы" }
                    }
                },
                {
                    "reportsProj",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "ProjectName", "Проект" },
                        { "HoursTotal", "Часы" }
                    }
                },
                {
                    "dashboardRecent",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "EmployeeName", "Сотрудник" },
                        { "ProjectName", "Проект" },
                        { "WorkDate", "Дата" },
                        { "StartTime", "Начало" },
                        { "EndTime", "Окончание" },
                        { "TypeName", "Тип учёта" },
                        { "NetMinutes", "Чистое время, мин" }
                    }
                },
                {
                    "reportTeamPulse",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "DepartmentName", "Подразделение" },
                        { "EmployeeName", "Сотрудник" },
                        { "HoursTotal", "Часы" }
                    }
                },
                {
                    "reportProjectReel",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "StatusName", "Статус проекта" },
                        { "ProjectName", "Проект" },
                        { "HoursTotal", "Часы" }
                    }
                },
                {
                    "adminPermissions",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "PermissionKey", "Раздел / ключ" },
                        { "IsAllowed", "Разрешено" }
                    }
                }
            };

        /// <summary>Заголовок столбца для экспорта/печати (русский из карты или имя поля).</summary>
        public static string GetCaption(string mapKey, string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(mapKey) || !Maps.TryGetValue(mapKey, out var map))
            {
                return columnName;
            }

            return map.TryGetValue(columnName, out var ru) ? ru : columnName;
        }

        public static void ApplyAll(DataGridView grid, string mapKey)
        {
            if (grid == null || string.IsNullOrWhiteSpace(mapKey) || !Maps.ContainsKey(mapKey))
            {
                return;
            }

            var map = Maps[mapKey];
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (ShouldHideIdColumn(column.Name))
                {
                    column.Visible = false;
                    continue;
                }

                if (map.TryGetValue(column.Name, out var ru))
                {
                    column.HeaderText = ru;
                    column.Visible = true;
                }
                else
                {
                    column.Visible = false;
                }
            }
        }

        private static bool ShouldHideIdColumn(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (name.Equals("RecordId", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (name.EndsWith("Id", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}
