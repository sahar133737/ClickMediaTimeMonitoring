using System;
using System.Data;
using System.Windows.Forms;
using ClickMediaWorkTime.Security;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class EmployeesForm : Form
    {
        private const string HelpKey = "employees";

        private readonly DataGridView _grid;
        private readonly TextBox _txtSearch;
        private readonly ComboBox _cbActive;
        private DataTable _table;

        public EmployeesForm()
        {
            ThemeHelper.ApplyForm(this, "Клик Медиа — Сотрудники");
            Width = 1160;
            Height = 720;
            if (!RolePermissionService.HasPermission("module.employees"))
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
            btnAdd.Click += (s, e) => OpenEdit(null);
            btnEdit.Click += (s, e) => OpenEditCurrent();
            btnDel.Click += (s, e) => DeleteCurrent();
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp(HelpKey, this);
            btnClose.Click += (s, e) => Close();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Статус:", 14, 72, 120));
            _cbActive = new ComboBox { Left = 14, Top = 94, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbActive.Items.AddRange(new object[] { "Все", "Только активные", "Только неактивные" });
            _cbActive.SelectedIndex = 0;
            _cbActive.SelectedIndexChanged += (s, e) => Reload();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Поиск:", 250, 72, 120));
            _txtSearch = new TextBox { Left = 250, Top = 94, Width = 320 };
            var btnFind = new Button { Left = 578, Top = 90, Width = 110, Height = 32, Text = "Найти" };
            var btnReset = new Button { Left = 694, Top = 90, Width = 110, Height = 32, Text = "Сброс" };
            ThemeHelper.StylePrimary(btnFind);
            ThemeHelper.StyleSecondary(btnReset);
            btnFind.Click += (s, e) => Reload();
            btnReset.Click += (s, e) =>
            {
                _txtSearch.Clear();
                _cbActive.SelectedIndex = 0;
                Reload();
            };
            top.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel, btnHelp, btnClose, _cbActive, _txtSearch, btnFind, btnReset });

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
            Controls.Add(top);
            ModuleHelpProvider.BindF11(this, HelpKey);
            Load += (s, e) => Reload();
        }

        private void Reload()
        {
            try
            {
                _table = EmployeeService.GetFiltered(_txtSearch.Text, _cbActive.SelectedIndex);
                GridPresentation.Bind(_grid, _table, "employees");
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("EmployeesForm.Reload", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEdit(int? id)
        {
            try
            {
                using (var f = new EmployeeEditForm(id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        Reload();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("EmployeesForm.Edit", ex);
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

            OpenEdit(Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value));
        }

        private void DeleteCurrent()
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            if (MessageBox.Show(this, "Пометить сотрудника как удалённого?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                if (CurrentUserContext.EmployeeId.HasValue && id == CurrentUserContext.EmployeeId.Value)
                {
                    MessageBox.Show(this, "Нельзя удалить профиль, привязанный к текущему пользователю.", "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                EmployeeService.SoftDelete(id);
                AuditService.LogChange("Employees", "SOFT_DELETE", id.ToString(), null, null);
                Reload();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("EmployeesForm.Delete", ex);
                MessageBox.Show(this, ex.Message, "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
