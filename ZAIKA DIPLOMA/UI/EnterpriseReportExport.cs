using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClickMediaWorkTime.UI
{
    /// <summary>Шапка печатных форм и экспорт отчётов (Excel как HTML, PDF через «Microsoft Print to PDF»).</summary>
    internal static class EnterpriseReportExport
    {
        public const string CompanyLegalName = "ООО «Клик Медиа»";
        public const string CompanyActivity = "Учёт рабочего времени и проектов";
        public const string CompanyAddress = "Адрес: _________________________________";
        public const string CompanyInn = "ИНН / КПП: __________ / __________";

        public static void ExportExcelHtml(string filePath, DataTable table, string sheetTitle, string columnMapKey = null)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var sb = new StringBuilder();
            sb.Append("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">");
            sb.Append("<head><meta charset=\"utf-8\"/><title>").Append(EscapeHtml(sheetTitle)).Append("</title></head><body>");
            sb.Append("<h2>").Append(EscapeHtml(sheetTitle)).Append("</h2>");
            sb.Append("<table border=\"1\" cellspacing=\"0\" cellpadding=\"4\">");
            sb.Append("<tr style=\"background:#E8EAF6;font-weight:bold\">");
            foreach (DataColumn c in table.Columns)
            {
                var cap = GridHeaderMap.GetCaption(columnMapKey, c.ColumnName);
                sb.Append("<td>").Append(EscapeHtml(cap)).Append("</td>");
            }

            sb.Append("</tr>");
            foreach (DataRow row in table.Rows)
            {
                sb.Append("<tr>");
                foreach (DataColumn c in table.Columns)
                {
                    sb.Append("<td>").Append(EscapeHtml(row[c]?.ToString() ?? string.Empty)).Append("</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</table></body></html>");
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        public static bool TryExportPdf(string filePath, string reportTitle, string periodText, DataTable table, IWin32Window owner, string columnMapKey = null)
        {
            var printer = FindMicrosoftPrintToPdf();
            if (string.IsNullOrEmpty(printer))
            {
                MessageBox.Show(
                    owner,
                    "Принтер «Microsoft Print to PDF» не найден. Установите компонент Windows или используйте «Печать» и выберите PDF вручную.",
                    "Экспорт PDF",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            using (var doc = CreatePrintDocument(reportTitle, periodText, table, columnMapKey))
            {
                doc.PrinterSettings.PrinterName = printer;
                doc.PrinterSettings.PrintToFile = true;
                doc.PrinterSettings.PrintFileName = filePath;
                doc.PrintController = new StandardPrintController();
                doc.Print();
            }

            return true;
        }

        public static void ShowPrintPreview(string reportTitle, string periodText, DataTable table, IWin32Window owner, string columnMapKey = null)
        {
            using (var doc = CreatePrintDocument(reportTitle, periodText, table, columnMapKey))
            using (var dlg = new PrintPreviewDialog())
            {
                dlg.Document = doc;
                dlg.Width = 900;
                dlg.Height = 700;
                dlg.ShowDialog(owner);
            }
        }

        private static string FindMicrosoftPrintToPdf()
        {
            foreach (string s in PrinterSettings.InstalledPrinters)
            {
                if (s.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0
                    && s.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return s;
                }
            }

            return null;
        }

        private static PrintDocument CreatePrintDocument(string reportTitle, string periodText, DataTable table, string columnMapKey)
        {
            var doc = new PrintDocument();
            doc.DefaultPageSettings.Margins = new Margins(60, 60, 60, 60);
            var state = new PrintState(table, reportTitle, periodText, columnMapKey);
            doc.BeginPrint += (s, e) => state.Reset();
            doc.PrintPage += (s, e) => PrintPageBody(state, e);
            return doc;
        }

        private sealed class PrintState
        {
            public PrintState(DataTable table, string title, string period, string columnMapKey)
            {
                Table = table;
                Title = title;
                Period = period;
                ColumnMapKey = columnMapKey;
            }

            public DataTable Table { get; }
            public string Title { get; }
            public string Period { get; }
            public string ColumnMapKey { get; }
            public int RowIndex { get; set; }

            public void Reset()
            {
                RowIndex = 0;
            }
        }

        private static void PrintPageBody(PrintState st, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            var r = e.MarginBounds;
            float y = r.Top;
            var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold);
            var normal = new Font("Segoe UI", 9f);
            var small = new Font("Segoe UI", 8f);
            var colHeaderFont = new Font("Segoe UI", 8f, FontStyle.Bold);

            if (st.RowIndex == 0)
            {
                g.DrawString(CompanyLegalName, titleFont, Brushes.Black, r.Left, y);
                y += titleFont.GetHeight(g) + 4;
                g.DrawString(CompanyActivity, normal, Brushes.Black, r.Left, y);
                y += normal.GetHeight(g) + 2;
                g.DrawString(CompanyAddress, small, Brushes.Black, r.Left, y);
                y += small.GetHeight(g) + 2;
                g.DrawString(CompanyInn, small, Brushes.Black, r.Left, y);
                y += small.GetHeight(g) + 8;
                g.DrawLine(Pens.Black, r.Left, y, r.Right, y);
                y += 10;
                g.DrawString(st.Title, new Font("Segoe UI", 11f, FontStyle.Bold), Brushes.Black, r.Left, y);
                y += 22;
                g.DrawString("Период: " + st.Period, normal, Brushes.Black, r.Left, y);
                y += normal.GetHeight(g) + 10;
            }
            else
            {
                g.DrawString("Продолжение отчёта: " + st.Title, small, Brushes.DimGray, r.Left, y);
                y += small.GetHeight(g) + 8;
            }

            var colWidths = ComputeColumnWidths(st.Table, r.Width - 20);
            float x = r.Left;
            for (var i = 0; i < st.Table.Columns.Count; i++)
            {
                var rect = new RectangleF(x, y, colWidths[i], 22);
                g.FillRectangle(Brushes.LightGray, rect);
                g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
                var cap = GridHeaderMap.GetCaption(st.ColumnMapKey, st.Table.Columns[i].ColumnName);
                g.DrawString(cap, colHeaderFont, Brushes.Black, rect);
                x += colWidths[i];
            }

            y += 24;

            var lineH = normal.GetHeight(g) + 2;
            while (st.RowIndex < st.Table.Rows.Count)
            {
                if (y + lineH > r.Bottom - 100)
                {
                    e.HasMorePages = true;
                    DrawFooter(g, r, y + 6);
                    return;
                }

                float x2 = r.Left;
                var row = st.Table.Rows[st.RowIndex];
                for (var i = 0; i < st.Table.Columns.Count; i++)
                {
                    var txt = row[i]?.ToString() ?? string.Empty;
                    if (txt.Length > 80)
                    {
                        txt = txt.Substring(0, 77) + "…";
                    }

                    var rect = new RectangleF(x2, y, colWidths[i], lineH);
                    g.DrawRectangle(Pens.LightGray, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString(txt, normal, Brushes.Black, rect);
                    x2 += colWidths[i];
                }

                y += lineH;
                st.RowIndex++;
            }

            DrawSignatures(g, r.Bottom - 72, r.Left, r.Right);
            e.HasMorePages = false;
        }

        private static void DrawFooter(Graphics g, Rectangle r, float y)
        {
            using (var pen = new Pen(Color.Gray, 1))
            {
                g.DrawLine(pen, r.Left, y, r.Right, y);
            }
        }

        private static void DrawSignatures(Graphics g, float top, float left, float right)
        {
            var f = new Font("Segoe UI", 8f);
            var w = (right - left) / 2f;
            g.DrawString("Руководитель организации", f, Brushes.Black, left, top);
            g.DrawString("________________________ / ________________________", f, Brushes.Black, left, top + 16);
            g.DrawString("М.П.", f, Brushes.Black, left, top + 34);
            g.DrawString("Исполнитель (ответственный за отчёт)", f, Brushes.Black, left + w, top);
            g.DrawString("________________________ / ________________________", f, Brushes.Black, left + w, top + 16);
        }

        private static float[] ComputeColumnWidths(DataTable table, float totalWidth)
        {
            var n = Math.Max(1, table.Columns.Count);
            var w = totalWidth / n;
            return Enumerable.Repeat(w, n).ToArray();
        }

        private static string EscapeHtml(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
