using System;
using System.Data;
using System.Windows.Forms;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class PositionsForm : Form
    {
        private const string HelpKey = "positions";

        private readonly DataGridView _grid;
        private readonly TextBox _txtSearch;
        private DataTable _table;
        private DataView _view;

        public PositionsForm()
        {
            ThemeHelper.ApplyForm(this, "Клик Медиа — Должности");
            Width = 960;
            Height = 680;
            if (!RolePermissionService.HasPermission("module.positions"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа.", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = ThemeHelper.CreateCardGroup("Действия и поиск", 150);
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

            top.Controls.Add(ThemeHelper.FormFieldLabel("Поиск:", 14, 72, 120));
            _txtSearch = new TextBox { Left = 14, Top = 94, Width = 320 };
            var btnFind = new Button { Left = 342, Top = 90, Width = 110, Height = 32, Text = "Найти" };
            var btnReset = new Button { Left = 458, Top = 90, Width = 110, Height = 32, Text = "Сброс" };
            ThemeHelper.StylePrimary(btnFind);
            ThemeHelper.StyleSecondary(btnReset);
            btnFind.Click += (s, e) => ApplyFilter();
            btnReset.Click += (s, e) => { _txtSearch.Clear(); ApplyFilter(); };
            top.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel, btnHelp, btnClose, _txtSearch, btnFind, btnReset });

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

        private void ApplyFilter()
        {
            if (_view == null || _table == null)
            {
                return;
            }

            GridPresentation.ApplyCombinedFilter(_view, _table, _txtSearch.Text, null);
        }

        private void Reload()
        {
            try
            {
                _table = PositionService.GetAll();
                _view = GridPresentation.Bind(_grid, _table, "positions");
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("PositionsForm.Reload", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEdit(int? id)
        {
            try
            {
                using (var f = new PositionEditForm(id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        Reload();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("PositionsForm.Edit", ex);
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

            if (MessageBox.Show(this, "Удалить должность?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                PositionService.SoftDelete(id);
                AuditService.LogChange("Positions", "SOFT_DELETE", id.ToString(), null, null);
                Reload();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("PositionsForm.Delete", ex);
                MessageBox.Show(this, ex.Message, "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
