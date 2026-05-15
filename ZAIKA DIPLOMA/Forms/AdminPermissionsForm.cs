using System;
using System.Data;
using System.Windows.Forms;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class AdminPermissionsForm : Form
    {
        private const string HelpKey = "admin";

        private readonly ComboBox _cbRoles;
        private readonly DataGridView _grid;
        private DataView _view;

        public AdminPermissionsForm()
        {
            ThemeHelper.ApplyForm(this, "Клик Медиа — Права доступа");
            Width = 1020;
            Height = 760;
            if (!RolePermissionService.HasPermission("module.admin"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа.", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = ThemeHelper.CreateCardGroup("Роль и сохранение", 130);
            top.Controls.Add(ThemeHelper.FormFieldLabel("Роль:", 14, 26, 120));
            _cbRoles = new ComboBox { Left = 14, Top = 48, Width = 420, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnReload = new Button { Left = 444, Top = 44, Width = 130, Height = 34, Text = "Обновить" };
            var btnSave = new Button { Left = 584, Top = 44, Width = 160, Height = 34, Text = "Сохранить" };
            var btnHelp = new Button { Left = 754, Top = 44, Width = 140, Height = 34, Text = "Справка (F11)" };
            var btnClose = new Button { Left = 904, Top = 44, Width = 100, Height = 34, Text = "Закрыть" };
            ThemeHelper.StyleSecondary(btnReload);
            ThemeHelper.StylePrimary(btnSave);
            ThemeHelper.StyleAccentOutline(btnHelp);
            ThemeHelper.StyleButton(btnClose, ThemeHelper.Accent);
            _cbRoles.SelectedIndexChanged += (s, e) => ReloadGrid();
            btnReload.Click += (s, e) => ReloadGrid();
            btnSave.Click += BtnSave_Click;
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp(HelpKey, this);
            btnClose.Click += (s, e) => Close();
            top.Controls.AddRange(new Control[] { _cbRoles, btnReload, btnSave, btnHelp, btnClose });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ThemeHelper.StyleGrid(_grid);
            GridPresentation.EnsureSortHook(_grid);

            Controls.Add(_grid);
            Controls.Add(top);
            ModuleHelpProvider.BindF11(this, HelpKey);
            Load += AdminPermissionsForm_Load;
        }

        private void AdminPermissionsForm_Load(object sender, EventArgs e)
        {
            try
            {
                var roles = RolePermissionService.GetRoles();
                _cbRoles.DisplayMember = "Name";
                _cbRoles.ValueMember = "Id";
                _cbRoles.DataSource = roles;
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("AdminPermissions.Load", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReloadGrid()
        {
            try
            {
                if (_cbRoles.SelectedValue == null)
                {
                    return;
                }

                var roleId = Convert.ToInt32(_cbRoles.SelectedValue);
                var table = RolePermissionService.GetPermissionsByRole(roleId);
                _view = table.DefaultView;
                _grid.DataSource = _view;
                GridHeaderMap.ApplyAll(_grid, "adminPermissions");
                if (_grid.Columns.Contains("IsAllowed"))
                {
                    _grid.Columns["IsAllowed"].ReadOnly = false;
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("AdminPermissions.ReloadGrid", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cbRoles.SelectedValue == null)
                {
                    return;
                }

                var roleId = Convert.ToInt32(_cbRoles.SelectedValue);
                var view = _grid.DataSource as DataView;
                var table = view?.Table;
                if (table == null)
                {
                    return;
                }

                foreach (DataRow row in table.Rows)
                {
                    var key = row["PermissionKey"].ToString();
                    var allowed = (bool)row["IsAllowed"];
                    RolePermissionService.SavePermission(roleId, key, allowed);
                }

                AuditService.LogChange("RolePermissions", "BULK_UPDATE", roleId.ToString(), null, "permissions");
                MessageBox.Show(this, "Права сохранены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("AdminPermissions.Save", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
