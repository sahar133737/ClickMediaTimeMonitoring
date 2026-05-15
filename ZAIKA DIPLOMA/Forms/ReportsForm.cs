using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class ReportsForm : Form
    {
        private const string HelpKey = "reports";

        private const string TabTeamTitle = "Пульс команды";
        private const string TabTeamHint = "Группировка по подразделению и сотруднику, сумма часов за период, строка «ИТОГО».";
        private const string TabProjectTitle = "Маршрут проектов";
        private const string TabProjectHint = "Группировка по статусу проекта и названию проекта, сумма часов, итог по всем строкам.";

        private const string MapTeam = "reportTeamPulse";
        private const string MapProject = "reportProjectReel";

        private readonly DateTimePicker _dtFrom;
        private readonly DateTimePicker _dtTo;
        private readonly TabControl _tabs;
        private readonly DataGridView _gTeam;
        private readonly DataGridView _gProject;

        private DataTable _tTeam;
        private DataTable _tProject;

        public ReportsForm()
        {
            ThemeHelper.ApplyForm(this, "Клик Медиа — Отчёты");
            Width = 1320;
            Height = 860;
            if (!RolePermissionService.HasPermission("module.reports"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа.", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = ThemeHelper.CreateCardGroup("Период и обновление", 124);
            top.Controls.Add(ThemeHelper.FormFieldLabel("С даты:", 14, 24, 80));
            _dtFrom = new DateTimePicker { Left = 14, Top = 46, Width = 150, Format = DateTimePickerFormat.Short };
            top.Controls.Add(ThemeHelper.FormFieldLabel("По:", 180, 24, 80));
            _dtTo = new DateTimePicker { Left = 180, Top = 46, Width = 150, Format = DateTimePickerFormat.Short };
            var btnLoad = new Button { Left = 350, Top = 40, Width = 220, Height = 40, Text = "Сформировать отчёты" };
            var btnHelp = new Button { Left = 580, Top = 40, Width = 170, Height = 40, Text = "Справка (F11)" };
            var btnClose = new Button { Left = 760, Top = 40, Width = 130, Height = 40, Text = "Закрыть" };
            ThemeHelper.StylePrimary(btnLoad);
            ThemeHelper.StyleAccentOutline(btnHelp);
            ThemeHelper.StyleButton(btnClose, ThemeHelper.Accent);
            btnLoad.Click += (s, e) => LoadAll();
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp(HelpKey, this);
            btnClose.Click += (s, e) => Close();
            top.Controls.AddRange(new Control[] { _dtFrom, _dtTo, btnLoad, btnHelp, btnClose });

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = ThemeHelper.UiFont };
            _gTeam = CreateGrid();
            _gTeam.Tag = MapTeam;
            _gProject = CreateGrid();
            _gProject.Tag = MapProject;

            AddReportTab(TabTeamTitle, TabTeamHint, _gTeam);
            AddReportTab(TabProjectTitle, TabProjectHint, _gProject);

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 128, FixedPanel = FixedPanel.Panel1 };
            split.Panel1.Controls.Add(top);
            split.Panel2.Controls.Add(_tabs);

            Controls.Add(split);
            ModuleHelpProvider.BindF11(this, HelpKey);
            Load += (s, e) =>
            {
                _dtTo.Value = DateTime.Today;
                _dtFrom.Value = DateTime.Today.AddMonths(-1);
                LoadAll();
            };
        }

        private static DataGridView CreateGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            ThemeHelper.StyleGrid(g);
            GridPresentation.EnsureSortHook(g);
            return g;
        }

        private void AddReportTab(string title, string hint, DataGridView grid)
        {
            var page = new TabPage(title);
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var hintLbl = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 10, 12, 4),
                Text = hint,
                ForeColor = ThemeHelper.MutedText,
                Font = ThemeHelper.UiFont
            };

            var bar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10, 6, 10, 6)
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            var btnXls = new Button { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Text = "Excel (.xls)" };
            var btnPdf = new Button { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0), Text = "PDF" };
            var btnPrint = new Button { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Text = "Печать" };
            ThemeHelper.StyleReportOutline(btnXls);
            ThemeHelper.StyleReportOutline(btnPdf);
            ThemeHelper.StylePrimary(btnPrint);

            bar.Controls.Add(btnXls, 0, 0);
            bar.Controls.Add(btnPdf, 1, 0);
            bar.Controls.Add(btnPrint, 2, 0);

            var gh = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 4) };
            gh.Controls.Add(grid);

            tlp.Controls.Add(hintLbl, 0, 0);
            tlp.Controls.Add(bar, 0, 1);
            tlp.Controls.Add(gh, 0, 2);
            page.Controls.Add(tlp);
            _tabs.TabPages.Add(page);

            btnXls.Click += (s, e) => ExportExcel(grid, title);
            btnPdf.Click += (s, e) => ExportPdf(grid, title);
            btnPrint.Click += (s, e) => PrintCurrent(grid, title);
        }

        private DataTable TableFor(DataGridView g)
        {
            return g == _gTeam ? _tTeam : _tProject;
        }

        private static string ColumnMapKey(DataGridView grid)
        {
            return grid?.Tag as string;
        }

        private void ExportExcel(DataGridView grid, string title)
        {
            var t = TableFor(grid);
            if (t == null || t.Rows.Count == 0)
            {
                MessageBox.Show(this, "Нет данных.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "Excel HTML (*.xls)|*.xls";
                    dlg.FileName = SafeFileName(title) + ".xls";
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    EnterpriseReportExport.ExportExcelHtml(dlg.FileName, t.Copy(), title, ColumnMapKey(grid));
                    MessageBox.Show(this, "Файл сохранён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("Reports.ExportExcel", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportPdf(DataGridView grid, string title)
        {
            var t = TableFor(grid);
            if (t == null || t.Rows.Count == 0)
            {
                MessageBox.Show(this, "Нет данных.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "PDF (*.pdf)|*.pdf";
                    dlg.FileName = SafeFileName(title) + ".pdf";
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    var copy = t.Copy();
                    if (!EnterpriseReportExport.TryExportPdf(dlg.FileName, title, PeriodText(), copy, this, ColumnMapKey(grid)))
                    {
                        return;
                    }

                    MessageBox.Show(this, "PDF сохранён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("Reports.ExportPdf", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintCurrent(DataGridView grid, string title)
        {
            var t = TableFor(grid);
            if (t == null || t.Rows.Count == 0)
            {
                MessageBox.Show(this, "Нет данных.", "Печать", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                EnterpriseReportExport.ShowPrintPreview(title, PeriodText(), t.Copy(), this, ColumnMapKey(grid));
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("Reports.Print", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string PeriodText()
        {
            return _dtFrom.Value.ToString("dd.MM.yyyy") + " — " + _dtTo.Value.ToString("dd.MM.yyyy");
        }

        private static string SafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(name) ? "report" : name.Trim();
        }

        private void LoadAll()
        {
            try
            {
                if (_dtTo.Value.Date < _dtFrom.Value.Date)
                {
                    MessageBox.Show(this, "Дата «по» не может быть раньше даты «с».", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var f = _dtFrom.Value.Date;
                var t = _dtTo.Value.Date;
                _tTeam = ReportService.GetReportDepartmentEmployeeHours(f, t);
                _tProject = ReportService.GetReportStatusProjectHours(f, t);

                Bind(_gTeam, _tTeam, MapTeam);
                Bind(_gProject, _tProject, MapProject);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("ReportsForm.LoadAll", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Bind(DataGridView g, DataTable table, string mapKey)
        {
            var v = GridPresentation.Bind(g, table, mapKey);
            v.RowFilter = string.Empty;
        }
    }
}
