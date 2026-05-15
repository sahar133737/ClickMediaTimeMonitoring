using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace ClickMediaWorkTime.UI
{
    /// <summary>Привязка DataView, быстрый поиск по строкам, сортировка по клику на заголовок.</summary>
    internal static class GridPresentation
    {
        private sealed class SortHookMarker
        {
        }

        public static DataView Bind(DataGridView grid, DataTable table, string mapKey)
        {
            var view = table.DefaultView;
            grid.DataSource = view;
            GridHeaderMap.ApplyAll(grid, mapKey);
            return view;
        }

        /// <summary>Один раз на таблицу: сортировка читает актуальный DataSource как DataView.</summary>
        public static void EnsureSortHook(DataGridView grid)
        {
            if (grid.Tag is SortHookMarker)
            {
                return;
            }

            grid.Tag = new SortHookMarker();
            var sortCols = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            grid.ColumnHeaderMouseClick += (sender, e) =>
            {
                if (e.RowIndex != -1 || e.ColumnIndex < 0)
                {
                    return;
                }

                var g = (DataGridView)sender;
                var view = g.DataSource as DataView;
                if (view == null)
                {
                    return;
                }

                var col = g.Columns[e.ColumnIndex];
                var name = col.DataPropertyName;
                if (string.IsNullOrEmpty(name) || !col.Visible)
                {
                    return;
                }

                if (!sortCols.ContainsKey(name))
                {
                    sortCols[name] = true;
                }
                else
                {
                    sortCols[name] = !sortCols[name];
                }

                try
                {
                    view.Sort = name + (sortCols[name] ? " ASC" : " DESC");
                }
                catch (Exception)
                {
                    // игнорируем
                }
            };
        }

        public static void ApplyCombinedFilter(DataView view, DataTable schemaTable, string searchText, string extraAndFilter)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(extraAndFilter))
            {
                parts.Add("(" + extraAndFilter + ")");
            }

            var q = (searchText ?? string.Empty).Trim();
            if (q.Length > 0)
            {
                var quick = BuildQuickSearchExpression(schemaTable, q);
                if (!string.IsNullOrWhiteSpace(quick))
                {
                    parts.Add("(" + quick + ")");
                }
            }

            view.RowFilter = parts.Count == 0 ? string.Empty : string.Join(" AND ", parts);
        }

        private static string BuildQuickSearchExpression(DataTable table, string term)
        {
            var esc = term.Replace("'", "''");
            var ors = new List<string>();
            foreach (DataColumn c in table.Columns)
            {
                if (ShouldSkipSearchColumn(c))
                {
                    continue;
                }

                ors.Add($"CONVERT([{c.ColumnName}], 'System.String') LIKE '%{esc}%'");
            }

            return ors.Count == 0 ? string.Empty : string.Join(" OR ", ors);
        }

        private static bool ShouldSkipSearchColumn(DataColumn c)
        {
            var t = c.DataType;
            if (t == typeof(byte[]) || t == typeof(object))
            {
                return true;
            }

            if (t == typeof(bool))
            {
                return true;
            }

            return false;
        }
    }
}
