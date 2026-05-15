using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using ClickMediaWorkTime.Security;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class MainMenuForm : Form
    {
        private const string HelpKey = "mainmenu";

        private readonly Label _lblEmp;
        private readonly Label _lblProj;
        private readonly Label _lblHours;
        private readonly DataGridView _gridRecent;
        private readonly Chart _chartDays;
        private DataTable _recentTable;
        private DataView _recentView;

        public MainMenuForm()
        {
            Width = 1420;
            Height = 920;
            StartPosition = FormStartPosition.CenterScreen;
            ThemeHelper.ApplyForm(this, "Клик Медиа — Главное меню");
            BackColor = ThemeHelper.ShellBg;
            KeyPreview = true;

            var sidebar = ThemeHelper.CreateSidebar(322);
            sidebar.BackColor = ThemeHelper.ShellPanel;

            const int sx = 20;
            const int sw = 282;
            var brand = new Label
            {
                Left = sx,
                Top = 22,
                Width = sw,
                Height = 28,
                ForeColor = ThemeHelper.Mint,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                Text = "КЛИК МЕДИА",
                AutoEllipsis = true
            };
            var title = new Label
            {
                Left = sx,
                Top = 52,
                Width = sw,
                Height = 34,
                ForeColor = Color.White,
                Font = ThemeHelper.HeroFont,
                Text = "Главное меню",
                AutoEllipsis = true
            };
            var tagline = new Label
            {
                Left = sx,
                Top = 90,
                Width = sw,
                Height = 48,
                ForeColor = Color.FromArgb(190, 198, 220),
                Font = ThemeHelper.UiFont,
                Text = "Учёт времени и проектов\nдля digital‑команды",
                AutoEllipsis = true
            };
            var user = new Label
            {
                Left = sx,
                Top = 146,
                Width = sw,
                Height = 78,
                ForeColor = Color.FromArgb(210, 216, 235),
                Font = ThemeHelper.UiFont,
                Text = $"Пользователь:\n{CurrentUserContext.FullName}\nРоль: {CurrentUserContext.RoleDisplayName}",
                AutoEllipsis = true
            };
            sidebar.Controls.Add(brand);
            sidebar.Controls.Add(title);
            sidebar.Controls.Add(tagline);
            sidebar.Controls.Add(user);

            int y = 236;
            const int navH = 44;
            const int navGap = 46;
            const int navW = 282;
            AddNav(sidebar, sx, "Подразделения", y, navW, navH, "module.departments", () => Open(new DepartmentsForm())); y += navGap;
            AddNav(sidebar, sx, "Должности", y, navW, navH, "module.positions", () => Open(new PositionsForm())); y += navGap;
            AddNav(sidebar, sx, "Сотрудники", y, navW, navH, "module.employees", () => Open(new EmployeesForm())); y += navGap;
            AddNav(sidebar, sx, "Проекты", y, navW, navH, "module.projects", () => Open(new ProjectsForm())); y += navGap;
            AddNav(sidebar, sx, "Учёт времени", y, navW, navH, "module.worktime", () => Open(new WorkTimeEntriesForm())); y += navGap;
            AddNav(sidebar, sx, "Пользователи", y, navW, navH, "module.users", () => Open(new UsersForm())); y += navGap;
            AddNav(sidebar, sx, "Отчёты", y, navW, navH, "module.reports", () => Open(new ReportsForm())); y += navGap;
            AddNav(sidebar, sx, "Резервные копии", y, navW, navH, "module.backups", () => Open(new BackupForm())); y += navGap;
            AddNav(sidebar, sx, "Журнал аудита", y, navW, navH, "module.audit", () => Open(new AuditLogForm())); y += navGap;
            AddNav(sidebar, sx, "Права доступа", y, navW, navH, "module.admin", () => Open(new AdminPermissionsForm())); y += navGap;

            var btnHelp = new Button { Left = sx, Top = y + 6, Width = navW, Height = navH, Text = "Справка (F11)" };
            ThemeHelper.StyleAccentOutline(btnHelp);
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp(HelpKey, this);
            sidebar.Controls.Add(btnHelp);

            var btnExit = new Button { Left = sx, Top = y + 6 + navGap, Width = navW, Height = navH, Text = "Выход" };
            ThemeHelper.StyleDanger(btnExit);
            btnExit.Click += (s, e) => Close();
            sidebar.Controls.Add(btnExit);

            var content = new Panel { Dock = DockStyle.Fill, BackColor = ThemeHelper.Surface, Padding = new Padding(20) };

            var pageTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = ThemeHelper.HeroFont,
                ForeColor = ThemeHelper.Text,
                Text = "Главное меню"
            };

            var kpi = new TableLayoutPanel { Dock = DockStyle.Top, Height = 148, ColumnCount = 3, RowCount = 1 };
            kpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            _lblEmp = AddKpi(kpi, 0, "Активные сотрудники", () => Open(new EmployeesForm()));
            _lblProj = AddKpi(kpi, 1, "Активные проекты", () => Open(new ProjectsForm()));
            _lblHours = AddKpi(kpi, 2, "Часы за 30 дней", () => Open(new WorkTimeEntriesForm()));

            var chartsPanel = new Panel { Dock = DockStyle.Top, Height = 360, BackColor = ThemeHelper.Surface };

            _chartDays = BuildChart(
                "Часы по дням (14 дней)",
                SeriesChartType.Line,
                ThemeHelper.Mint,
                "Дата",
                "Часы");
            _chartDays.MouseClick += ChartDays_MouseClick;
            _chartDays.MouseMove += Chart_MouseMove;

            var hostDays = WrapChartQuickActions(
                _chartDays,
                "Учёт времени",
                () => Open(new WorkTimeEntriesForm()),
                "Проекты",
                () => Open(new ProjectsForm()));
            hostDays.Dock = DockStyle.Fill;
            chartsPanel.Controls.Add(hostDays);

            var lblGrid = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = ThemeHelper.UiFontSemi,
                ForeColor = ThemeHelper.Text,
                Text = "Последние записи учёта времени"
            };

            _gridRecent = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            ThemeHelper.StyleGrid(_gridRecent);
            _gridRecent.CellDoubleClick += GridRecent_CellDoubleClick;
            GridPresentation.EnsureSortHook(_gridRecent);

            content.Controls.Add(_gridRecent);
            content.Controls.Add(lblGrid);
            content.Controls.Add(chartsPanel);
            content.Controls.Add(kpi);
            content.Controls.Add(pageTitle);

            Controls.Add(content);
            Controls.Add(sidebar);
            ModuleHelpProvider.BindF11(this, HelpKey);
            FormClosing += MainMenuForm_FormClosing;
            Load += MainMenuForm_Load;
        }

        private void TryOpenReports()
        {
            if (!RolePermissionService.HasPermission("module.reports"))
            {
                MessageBox.Show(this, "Нет доступа к отчётам для вашей роли.", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Open(new ReportsForm());
        }

        /// <summary>Панель кнопок над диаграммой для быстрого перехода в модули и обновления.</summary>
        private Panel WrapChartQuickActions(
            Chart chart,
            string primaryCaption,
            Action primaryNav,
            string secondaryCaption,
            Action secondaryNav,
            bool secondaryEnabled = true)
        {
            var outer = new Panel { Dock = DockStyle.Fill, BackColor = ThemeHelper.Surface };

            void Safe(Action a)
            {
                try
                {
                    a?.Invoke();
                }
                catch (Exception ex)
                {
                    ErrorLogService.LogUiException("MainMenu.QuickChart", ex);
                    MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(4, 2, 4, 6),
                BackColor = ThemeHelper.Surface
            };

            var b1 = new Button
            {
                Text = primaryCaption,
                AutoSize = true,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(4, 2, 4, 2),
                MinimumSize = new Size(120, 34)
            };
            ThemeHelper.StyleReportOutline(b1);
            b1.Click += (s, e) => Safe(primaryNav);

            var b2 = new Button
            {
                Text = secondaryCaption,
                AutoSize = true,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(4, 2, 4, 2),
                MinimumSize = new Size(120, 34),
                Enabled = secondaryEnabled
            };
            ThemeHelper.StyleReportOutline(b2);
            b2.Click += (s, e) => Safe(secondaryNav);

            var bRef = new Button
            {
                Text = "Обновить панель",
                AutoSize = true,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(4, 2, 4, 2),
                MinimumSize = new Size(130, 34)
            };
            ThemeHelper.StylePrimary(bRef);
            bRef.Click += (s, e) => Safe(RefreshDashboard);

            bar.Controls.Add(b1);
            bar.Controls.Add(b2);
            bar.Controls.Add(bRef);

            outer.Controls.Add(bar);
            outer.Controls.Add(chart);
            chart.Dock = DockStyle.Fill;

            return outer;
        }

        private void MainMenuForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!AppPreferences.AutoBackupOnExit)
            {
                return;
            }

            if (!RolePermissionService.HasPermission("module.backups"))
            {
                return;
            }

            try
            {
                var path = BackupService.GetDefaultBackupFilePath($"ClickMediaTimeDB_exit_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                BackupService.CreateBackupToFile(path, "Автоматически при закрытии приложения", true);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("MainMenu.AutoBackupOnExit", ex);
            }
        }

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;
            var hit = chart.HitTest(e.X, e.Y);
            chart.Cursor = hit.ChartElementType == ChartElementType.DataPoint ? Cursors.Hand : Cursors.Default;
        }

        private void ChartDays_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                var chart = (Chart)sender;
                var hit = chart.HitTest(e.X, e.Y);
                if (hit.ChartElementType == ChartElementType.DataPoint && hit.Series != null && hit.PointIndex >= 0)
                {
                    if (hit.Series.Points[hit.PointIndex].Tag is DateTime dt)
                    {
                        WorkTimeEntriesForm.PendingWorkDate = dt.Date;
                        Open(new WorkTimeEntriesForm());
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("MainMenu.ChartDays", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void GridRecent_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || !_gridRecent.Columns.Contains("ProjectId"))
            {
                return;
            }

            if (!RolePermissionService.HasPermission("module.worktime"))
            {
                return;
            }

            var raw = _gridRecent.Rows[e.RowIndex].Cells["ProjectId"].Value;
            if (raw == null || raw == DBNull.Value)
            {
                return;
            }

            WorkTimeEntriesForm.PendingProjectId = Convert.ToInt32(raw);
            Open(new WorkTimeEntriesForm());
        }

        private static Chart BuildChart(
            string title,
            SeriesChartType type,
            Color seriesColor,
            string axisXTitle,
            string axisYTitle)
        {
            var chart = new Chart { Dock = DockStyle.Fill };
            var area = new ChartArea("main");
            area.BackColor = ThemeHelper.Card;
            area.InnerPlotPosition = new ElementPosition(8, 12, 88, 74);
            area.AxisX.Title = axisXTitle;
            area.AxisY.Title = axisYTitle;
            area.AxisX.TitleFont = ThemeHelper.UiFont;
            area.AxisY.TitleFont = ThemeHelper.UiFont;
            area.AxisX.TitleForeColor = ThemeHelper.MutedText;
            area.AxisY.TitleForeColor = ThemeHelper.MutedText;
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(225, 228, 236);
            area.AxisY.LabelStyle.Format = "#0.#";
            area.AxisX.LabelStyle.Enabled = true;
            area.AxisX.IsMarginVisible = true;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Angle = -45;
            area.AxisX.LabelStyle.Font = new Font(ThemeHelper.UiFont.FontFamily, 8.5f);
            chart.ChartAreas.Add(area);
            var series = new Series("s1")
            {
                ChartType = type,
                IsValueShownAsLabel = false,
                Color = seriesColor,
                BorderColor = seriesColor
            };
            chart.Series.Add(series);
            chart.Titles.Add(new Title(title, Docking.Top, ThemeHelper.UiFontSemi, ThemeHelper.Text));
            chart.BackColor = ThemeHelper.Card;
            return chart;
        }

        private void MainMenuForm_Load(object sender, EventArgs e)
        {
            RefreshDashboard();
        }

        private void RefreshDashboard()
        {
            try
            {
                _lblEmp.Text = DashboardService.GetActiveEmployeesCount().ToString();
                _lblProj.Text = DashboardService.GetActiveProjectsCount().ToString();
                _lblHours.Text = DashboardService.GetHoursSumLast30Days().ToString("0.##");

                FillDayChart();

                _recentTable = DashboardService.GetRecentTimeEntries(40);
                _recentView = _recentTable.DefaultView;
                _gridRecent.DataSource = _recentView;
                GridHeaderMap.ApplyAll(_gridRecent, "dashboardRecent");
                TuneRecentGridColumns();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("MainMenu.RefreshDashboard", ex);
                MessageBox.Show(this, "Не удалось обновить панель: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TuneRecentGridColumns()
        {
            foreach (DataGridViewColumn c in _gridRecent.Columns)
            {
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                var n = c.DataPropertyName ?? string.Empty;
                if (n.Equals("EmployeeName", StringComparison.OrdinalIgnoreCase)
                    || n.Equals("ProjectName", StringComparison.OrdinalIgnoreCase))
                {
                    c.MinimumWidth = 200;
                    c.FillWeight = 42;
                }
                else if (n.Equals("Comment", StringComparison.OrdinalIgnoreCase))
                {
                    c.MinimumWidth = 80;
                    c.FillWeight = 25;
                }
                else
                {
                    c.FillWeight = 18;
                }
            }
        }

        private void FillDayChart()
        {
            var series = _chartDays.Series["s1"];
            series.Points.Clear();
            series.IsValueShownAsLabel = false;
            var table = DashboardService.GetHoursByDayLast14Days();
            foreach (DataRow row in table.Rows)
            {
                var day = Convert.ToDateTime(row["WorkDay"]).Date;
                var label = day.ToString("dd.MM");
                var hours = Convert.ToDouble(row["HoursTotal"]);
                var i = series.Points.AddXY(label, hours);
                var p = series.Points[i];
                p.Tag = day;
                p.ToolTip = day.ToString("dd.MM.yyyy") + ": " + hours.ToString("0.##") + " ч"
                    + Environment.NewLine + "Клик — учёт времени за эту дату";
                p.Label = string.Empty;
            }

            var ax = _chartDays.ChartAreas[0].AxisX;
            ax.Interval = 1;
        }

        private Label AddKpi(TableLayoutPanel parent, int col, string title, Action onOpen)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = ThemeHelper.Card, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10, 10, 10, 12),
                BackColor = ThemeHelper.Card
            };
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitle = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                ForeColor = ThemeHelper.MutedText,
                Font = ThemeHelper.UiFont,
                Text = title,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.TopLeft
            };
            var lblValue = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 2),
                Font = new Font(ThemeHelper.HeroFont.FontFamily, 24f, FontStyle.Bold),
                ForeColor = ThemeHelper.Brand,
                Text = "0",
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.TopLeft
            };
            void NavClick(object s, EventArgs e)
            {
                if (onOpen == null)
                {
                    return;
                }

                try
                {
                    onOpen();
                }
                catch (Exception ex)
                {
                    ErrorLogService.LogUiException("MainMenu.Kpi:" + title, ex);
                    MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            panel.Click += NavClick;
            lblTitle.Click += NavClick;
            lblValue.Click += NavClick;
            inner.Controls.Add(lblTitle, 0, 0);
            inner.Controls.Add(lblValue, 0, 1);
            panel.Controls.Add(inner);
            parent.Controls.Add(panel, col, 0);
            return lblValue;
        }

        private void AddNav(Panel sidebar, int navLeft, string text, int top, int width, int height, string permission, Action onClick)
        {
            var btn = new Button
            {
                Left = navLeft,
                Top = top,
                Width = width,
                Height = height,
                Text = text,
                Enabled = RolePermissionService.HasPermission(permission)
            };
            ThemeHelper.StyleGhostOnDark(btn);
            btn.Click += (s, e) =>
            {
                try
                {
                    onClick();
                }
                catch (Exception ex)
                {
                    ErrorLogService.LogUiException("MainMenu.Nav:" + text, ex);
                    MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            sidebar.Controls.Add(btn);
        }

        private void Open(Form f)
        {
            using (f)
            {
                f.ShowDialog(this);
            }

            RefreshDashboard();
        }
    }
}
