using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class WorkTimeEntriesForm : Form
    {
        private const string HelpKey = "worktime";

        public static int? PendingProjectId;
        public static DateTime? PendingWorkDate;

        internal static int? LastFilterProjectId;
        internal static DateTime? LastFilterWorkDate;

        private readonly DataGridView _grid;
        private readonly Label _lblFilter;
        private readonly TextBox _txtSearch;
        private readonly ComboBox _cbProject;
        private readonly DateTimePicker _dtFrom;
        private readonly DateTimePicker _dtTo;
        private DataTable _table;
        private DataView _view;

        public WorkTimeEntriesForm()
        {
            ThemeHelper.ApplyForm(this, "Клик Медиа — Учёт рабочего времени");
            Width = 1240;
            Height = 760;
            if (!RolePermissionService.HasPermission("module.worktime"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа.", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            _lblFilter = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = ThemeHelper.MutedText,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = ThemeHelper.UiFont,
                Padding = new Padding(12, 6, 12, 0)
            };

            var top = ThemeHelper.CreateCardGroup("Действия, фильтры и поиск", 210);
            var btnAdd = new Button { Left = 14, Top = 26, Width = 130, Height = 34, Text = "Добавить" };
            var btnEdit = new Button { Left = 152, Top = 26, Width = 130, Height = 34, Text = "Изменить" };
            var btnDel = new Button { Left = 290, Top = 26, Width = 160, Height = 34, Text = "Удалить" };
            var btnResetFilter = new Button { Left = 458, Top = 26, Width = 200, Height = 34, Text = "Сбросить фильтры" };
            var btnHelp = new Button { Left = 666, Top = 26, Width = 170, Height = 34, Text = "Справка (F11)" };
            var btnClose = new Button { Left = 844, Top = 26, Width = 120, Height = 34, Text = "Закрыть" };
            ThemeHelper.StylePrimary(btnAdd);
            ThemeHelper.StyleSecondary(btnEdit);
            ThemeHelper.StyleDanger(btnDel);
            ThemeHelper.StyleSecondary(btnResetFilter);
            ThemeHelper.StyleAccentOutline(btnHelp);
            ThemeHelper.StyleButton(btnClose, ThemeHelper.Accent);
            btnAdd.Click += (s, e) => OpenEdit(null);
            btnEdit.Click += (s, e) => OpenEditCurrent();
            btnDel.Click += (s, e) => DeleteCurrent();
            btnResetFilter.Click += (s, e) => ResetUiFilters();
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp(HelpKey, this);
            btnClose.Click += (s, e) => Close();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Проект:", 14, 72, 120));
            _cbProject = new ComboBox { Left = 14, Top = 94, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, MaxDropDownItems = 14 };
            _cbProject.SelectedIndexChanged += (s, e) => ApplyFilter();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Период (дата работы):", 350, 72, 220));
            _dtFrom = new DateTimePicker { Left = 350, Top = 94, Width = 140, Format = DateTimePickerFormat.Short };
            _dtTo = new DateTimePicker { Left = 498, Top = 94, Width = 140, Format = DateTimePickerFormat.Short };
            _dtFrom.ValueChanged += (s, e) => ApplyFilter();
            _dtTo.ValueChanged += (s, e) => ApplyFilter();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Поиск по таблице:", 14, 132, 200));
            _txtSearch = new TextBox { Left = 14, Top = 154, Width = 360 };
            var btnFind = new Button { Left = 382, Top = 150, Width = 110, Height = 32, Text = "Найти" };
            var btnResetSearch = new Button { Left = 498, Top = 150, Width = 110, Height = 32, Text = "Сброс" };
            ThemeHelper.StylePrimary(btnFind);
            ThemeHelper.StyleSecondary(btnResetSearch);
            btnFind.Click += (s, e) => ApplyFilter();
            btnResetSearch.Click += (s, e) => { _txtSearch.Clear(); ApplyFilter(); };

            top.Controls.AddRange(new Control[]
            {
                btnAdd, btnEdit, btnDel, btnResetFilter, btnHelp, btnClose,
                _cbProject, _dtFrom, _dtTo, _txtSearch, btnFind, btnResetSearch
            });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ThemeHelper.StyleGrid(_grid);
            _grid.CellDoubleClick += (s, e) => OpenEditCurrent();
            GridPresentation.EnsureSortHook(_grid);

            Controls.Add(_grid);
            Controls.Add(_lblFilter);
            Controls.Add(top);
            ModuleHelpProvider.BindF11(this, HelpKey);
            Load += WorkTimeEntriesForm_Load;
        }

        private void WorkTimeEntriesForm_Load(object sender, EventArgs e)
        {
            EnsureProjectCombo();
            _dtTo.Value = DateTime.Today;
            _dtFrom.Value = DateTime.Today.AddMonths(-1);
            Reload();
        }

        private void EnsureProjectCombo()
        {
            if (_cbProject.DataSource != null)
            {
                return;
            }

            var p = ProjectService.GetLookupActive();
            var dt = p.Copy();
            var r = dt.NewRow();
            r["Id"] = -1;
            r["DisplayName"] = "Все проекты";
            dt.Rows.InsertAt(r, 0);
            _cbProject.DisplayMember = "DisplayName";
            _cbProject.ValueMember = "Id";
            _cbProject.DataSource = dt;
            _cbProject.SelectedIndex = 0;
        }

        private void ResetUiFilters()
        {
            PendingProjectId = null;
            PendingWorkDate = null;
            LastFilterProjectId = null;
            LastFilterWorkDate = null;
            _txtSearch.Clear();
            if (_cbProject.DataSource != null)
            {
                _cbProject.SelectedValue = -1;
            }

            _dtTo.Value = DateTime.Today;
            _dtFrom.Value = DateTime.Today.AddMonths(-1);
            Reload();
        }

        private string BuildExtraFilter()
        {
            var parts = new List<string>();
            if (_dtTo.Value.Date >= _dtFrom.Value.Date)
            {
                parts.Add($"WorkDate >= #{_dtFrom.Value.Date:MM/dd/yyyy}# AND WorkDate <= #{_dtTo.Value.Date:MM/dd/yyyy}#");
            }

            var pidFromCombo = _cbProject.SelectedValue != null ? Convert.ToInt32(_cbProject.SelectedValue) : -1;
            if (pidFromCombo > 0)
            {
                parts.Add("ProjectId = " + pidFromCombo);
            }
            else if (LastFilterProjectId.HasValue)
            {
                parts.Add("ProjectId = " + LastFilterProjectId.Value);
            }

            return parts.Count == 0 ? null : string.Join(" AND ", parts);
        }

        private void ApplyFilter()
        {
            if (_view == null || _table == null)
            {
                return;
            }

            GridPresentation.ApplyCombinedFilter(_view, _table, _txtSearch.Text, BuildExtraFilter());
            UpdateFilterHint();
        }

        private void UpdateFilterHint()
        {
            var bits = new List<string>();
            if (_cbProject.SelectedValue != null && Convert.ToInt32(_cbProject.SelectedValue) > 0)
            {
                bits.Add("проект из списка");
            }
            else if (LastFilterProjectId.HasValue)
            {
                bits.Add("проект с панели");
            }

            bits.Add($"период {_dtFrom.Value:dd.MM.yyyy}—{_dtTo.Value:dd.MM.yyyy}");
            _lblFilter.Text = "Активные фильтры: " + string.Join(", ", bits) + ". Сортировка — клик по заголовку столбца.";
        }

        private void Reload()
        {
            try
            {
                var snapProj = PendingProjectId;
                var snapDate = PendingWorkDate;
                PendingProjectId = null;
                PendingWorkDate = null;

                if (snapDate.HasValue)
                {
                    LastFilterWorkDate = snapDate;
                    _dtFrom.Value = snapDate.Value.Date;
                    _dtTo.Value = snapDate.Value.Date;
                }

                if (snapProj.HasValue)
                {
                    LastFilterProjectId = snapProj;
                }

                EnsureProjectCombo();
                if (LastFilterProjectId.HasValue && _cbProject.DataSource is DataTable cdt)
                {
                    var found = false;
                    foreach (DataRow row in cdt.Rows)
                    {
                        if (row["Id"] != DBNull.Value && Convert.ToInt32(row["Id"]) == LastFilterProjectId.Value)
                        {
                            _cbProject.SelectedValue = LastFilterProjectId.Value;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        _cbProject.SelectedValue = -1;
                    }
                }

                _table = WorkTimeEntryService.GetAll();
                _view = GridPresentation.Bind(_grid, _table, "worktime");
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("WorkTimeEntriesForm.Reload", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEdit(long? id)
        {
            try
            {
                using (var f = new WorkTimeEntryEditForm(id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        Reload();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("WorkTimeEntriesForm.Edit", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEditCurrent()
        {
            if (_grid.CurrentRow == null)
            {
                MessageBox.Show(this, "Выберите строку.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenEdit(Convert.ToInt64(_grid.CurrentRow.Cells["Id"].Value));
        }

        private void DeleteCurrent()
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            if (MessageBox.Show(this, "Удалить запись учёта времени?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var id = Convert.ToInt64(_grid.CurrentRow.Cells["Id"].Value);
                WorkTimeEntryService.Delete(id);
                AuditService.LogChange("WorkTimeEntries", "DELETE", id.ToString(), null, null);
                Reload();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("WorkTimeEntriesForm.Delete", ex);
                MessageBox.Show(this, ex.Message, "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
