using System;
using System.Data;
using System.Windows.Forms;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class AuditLogForm : Form
    {
        private const string HelpKey = "audit";

        private readonly DateTimePicker _dtFrom;
        private readonly DateTimePicker _dtTo;
        private readonly TextBox _txtSearch;
        private readonly DataGridView _grid;
        private DataTable _table;
        private DataView _view;

        public AuditLogForm()
        {
            ThemeHelper.ApplyForm(this, "Клик Медиа — Журнал аудита");
            Width = 1220;
            Height = 760;
            if (!RolePermissionService.HasPermission("module.audit"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа.", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = ThemeHelper.CreateCardGroup("Период, поиск и загрузка", 150);
            top.Controls.Add(ThemeHelper.FormFieldLabel("С даты:", 14, 26, 80));
            _dtFrom = new DateTimePicker { Left = 14, Top = 48, Width = 150, Format = DateTimePickerFormat.Short };
            top.Controls.Add(ThemeHelper.FormFieldLabel("По:", 180, 26, 80));
            _dtTo = new DateTimePicker { Left = 180, Top = 48, Width = 150, Format = DateTimePickerFormat.Short };
            top.Controls.Add(ThemeHelper.FormFieldLabel("Поиск по журналу:", 350, 26, 200));
            _txtSearch = new TextBox { Left = 350, Top = 48, Width = 320 };
            var btnLoad = new Button { Left = 680, Top = 42, Width = 150, Height = 34, Text = "Загрузить" };
            var btnFind = new Button { Left = 838, Top = 42, Width = 120, Height = 34, Text = "Найти" };
            var btnReset = new Button { Left = 966, Top = 42, Width = 110, Height = 34, Text = "Сброс" };
            var btnHelp = new Button { Left = 14, Top = 92, Width = 170, Height = 34, Text = "Справка (F11)" };
            var btnClose = new Button { Left = 192, Top = 92, Width = 120, Height = 34, Text = "Закрыть" };
            ThemeHelper.StylePrimary(btnLoad);
            ThemeHelper.StylePrimary(btnFind);
            ThemeHelper.StyleSecondary(btnReset);
            ThemeHelper.StyleAccentOutline(btnHelp);
            ThemeHelper.StyleButton(btnClose, ThemeHelper.Accent);
            btnLoad.Click += (s, e) => ReloadFromDb();
            btnFind.Click += (s, e) => ApplyFilter();
            btnReset.Click += (s, e) => { _txtSearch.Clear(); ApplyFilter(); };
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp(HelpKey, this);
            btnClose.Click += (s, e) => Close();
            top.Controls.AddRange(new Control[] { _dtFrom, _dtTo, _txtSearch, btnLoad, btnFind, btnReset, btnHelp, btnClose });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ThemeHelper.StyleGrid(_grid);
            GridPresentation.EnsureSortHook(_grid);

            Controls.Add(_grid);
            Controls.Add(top);
            ModuleHelpProvider.BindF11(this, HelpKey);
            Load += (s, e) =>
            {
                _dtTo.Value = DateTime.Today;
                _dtFrom.Value = DateTime.Today.AddDays(-30);
                ReloadFromDb();
            };
        }

        private void ReloadFromDb()
        {
            try
            {
                if (_dtTo.Value < _dtFrom.Value)
                {
                    MessageBox.Show(this, "Некорректный период.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _table = AuditService.GetAuditLog(_dtFrom.Value.Date, _dtTo.Value.Date.AddDays(1).AddSeconds(-1));
                _view = GridPresentation.Bind(_grid, _table, "audit");
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("AuditLogForm.Reload", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_view == null || _table == null)
            {
                return;
            }

            GridPresentation.ApplyCombinedFilter(_view, _table, _txtSearch.Text, null);
        }
    }
}
