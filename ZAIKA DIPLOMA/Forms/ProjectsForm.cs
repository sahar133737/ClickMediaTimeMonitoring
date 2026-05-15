using System;
using System.Data;
using System.Windows.Forms;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class ProjectsForm : Form
    {
        private const string HelpKey = "projects";

        private readonly DataGridView _grid;
        private readonly TextBox _txtSearch;
        private readonly ComboBox _cbStatus;
        private DataTable _table;
        private DataView _view;

        public ProjectsForm()
        {
            ThemeHelper.ApplyForm(this, "Клик Медиа — Проекты");
            Width = 1160;
            Height = 720;
            if (!RolePermissionService.HasPermission("module.projects"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа.", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = ThemeHelper.CreateCardGroup("Действия, фильтр и поиск", 170);
            var btnAdd = new Button { Left = 14, Top = 26, Width = 130, Height = 34, Text = "Добавить" };
            var btnEdit = new Button { Left = 152, Top = 26, Width = 130, Height = 34, Text = "Изменить" };
            var btnDel = new Button { Left = 290, Top = 26, Width = 160, Height = 34, Text = "Удалить" };
            var btnHelp = new Button { Left = 458, Top = 26, Width = 170, Height = 34, Text = "Справка (F11)" };
            var btnClose = new Button { Left = 636, Top = 26, Width = 120, Height = 34, Text = "Закрыть" };
            ThemeHelper.StylePrimary(btnAdd);
            ThemeHelper.StyleSecondary(btnEdit);
            ThemeHelper.StyleDanger(btnDel);
            ThemeHelper.StyleAccentOutline(btnHelp);
            ThemeHelper.StyleButton(btnClose, ThemeHelper.Accent);
            btnAdd.Click += (s, e) => Open(null);
            btnEdit.Click += (s, e) => OpenCurrent();
            btnDel.Click += (s, e) => DeleteCurrent();
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp(HelpKey, this);
            btnClose.Click += (s, e) => Close();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Статус проекта:", 14, 72, 160));
            _cbStatus = new ComboBox { Left = 14, Top = 94, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbStatus.SelectedIndexChanged += (s, e) => ApplyFilter();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Поиск:", 290, 72, 120));
            _txtSearch = new TextBox { Left = 290, Top = 94, Width = 320 };
            var btnFind = new Button { Left = 618, Top = 90, Width = 110, Height = 32, Text = "Найти" };
            var btnReset = new Button { Left = 734, Top = 90, Width = 110, Height = 32, Text = "Сброс" };
            ThemeHelper.StylePrimary(btnFind);
            ThemeHelper.StyleSecondary(btnReset);
            btnFind.Click += (s, e) => ApplyFilter();
            btnReset.Click += (s, e) =>
            {
                _txtSearch.Clear();
                if (_cbStatus.Items.Count > 0)
                {
                    _cbStatus.SelectedIndex = 0;
                }

                ApplyFilter();
            };
            top.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel, btnHelp, btnClose, _cbStatus, _txtSearch, btnFind, btnReset });

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
            _grid.CellDoubleClick += (s, e) => OpenCurrent();
            GridPresentation.EnsureSortHook(_grid);

            Controls.Add(_grid);
            Controls.Add(top);
            ModuleHelpProvider.BindF11(this, HelpKey);
            Load += (s, e) => Reload();
        }

        private void LoadStatusCombo()
        {
            if (_cbStatus.DataSource != null)
            {
                return;
            }

            var st = ProjectService.GetStatuses();
            var dt = st.Copy();
            var r = dt.NewRow();
            r["Id"] = -1;
            r["Name"] = "Все статусы";
            dt.Rows.InsertAt(r, 0);
            _cbStatus.DisplayMember = "Name";
            _cbStatus.ValueMember = "Id";
            _cbStatus.DataSource = dt;
            _cbStatus.SelectedIndex = 0;
        }

        private string BuildStatusFilter()
        {
            if (_cbStatus.SelectedValue == null)
            {
                return null;
            }

            var id = Convert.ToInt32(_cbStatus.SelectedValue);
            return id > 0 ? "StatusId = " + id : null;
        }

        private void ApplyFilter()
        {
            if (_view == null || _table == null)
            {
                return;
            }

            GridPresentation.ApplyCombinedFilter(_view, _table, _txtSearch.Text, BuildStatusFilter());
        }

        private void Reload()
        {
            try
            {
                LoadStatusCombo();
                _table = ProjectService.GetAll();
                _view = GridPresentation.Bind(_grid, _table, "projects");
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("ProjectsForm.Reload", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Open(int? id)
        {
            try
            {
                using (var f = new ProjectEditForm(id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        Reload();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("ProjectsForm.Edit", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenCurrent()
        {
            if (_grid.CurrentRow == null)
            {
                MessageBox.Show(this, "Выберите строку.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Open(Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value));
        }

        private void DeleteCurrent()
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            if (MessageBox.Show(this, "Пометить проект как удалённый?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                ProjectService.SoftDelete(id);
                AuditService.LogChange("Projects", "SOFT_DELETE", id.ToString(), null, null);
                Reload();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("ProjectsForm.Delete", ex);
                MessageBox.Show(this, ex.Message, "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
