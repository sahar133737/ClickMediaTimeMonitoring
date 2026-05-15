using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class UserEditForm : Form
    {
        private readonly int? _id;
        private readonly TextBox _txtLogin;
        private readonly TextBox _txtName;
        private readonly TextBox _txtPassword;
        private readonly ComboBox _cbRole;
        private readonly ComboBox _cbEmp;
        private readonly CheckBox _chkActive;

        public UserEditForm(int? id)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, id.HasValue ? "Пользователь — редактирование" : "Пользователь — создание");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 560;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;

            int y = 10;
            Controls.Add(ThemeHelper.FormFieldLabel("Логин *:", 18, y, 200));
            _txtLogin = new TextBox { Left = 18, Top = y + 22, Width = 500, MaxLength = 64 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("ФИО *:", 18, y, 200));
            _txtName = new TextBox { Left = 18, Top = y + 22, Width = 500, MaxLength = 200 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel(_id.HasValue ? "Новый пароль (оставьте пустым, чтобы не менять):" : "Пароль *:", 18, y, 420));
            _txtPassword = new TextBox { Left = 18, Top = y + 22, Width = 500, PasswordChar = '*', MaxLength = 128 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Роль *:", 18, y, 200));
            _cbRole = new ComboBox { Left = 18, Top = y + 22, Width = 500, DropDownStyle = ComboBoxStyle.DropDownList };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Сотрудник (опционально):", 18, y, 360));
            _cbEmp = new ComboBox { Left = 18, Top = y + 22, Width = 500, DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, MaxDropDownItems = 15 };
            y += 62;
            _chkActive = new CheckBox { Left = 18, Top = y, Width = 260, Text = "Учётная запись активна", Checked = true };
            y += 62;

            var btnOk = new Button { Left = 18, Top = y + 6, Width = 170, Height = 36, Text = "Сохранить" };
            var btnCancel = new Button { Left = 200, Top = y + 6, Width = 170, Height = 36, Text = "Отмена", DialogResult = DialogResult.Cancel };
            ThemeHelper.StyleButton(btnOk, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnOk.Click += BtnOk_Click;

            Controls.AddRange(new Control[] { _txtLogin, _txtName, _txtPassword, _cbRole, _cbEmp, _chkActive, btnOk, btnCancel });
            CancelButton = btnCancel;
            Load += UserEditForm_Load;
        }

        private void UserEditForm_Load(object sender, EventArgs e)
        {
            try
            {
                var roles = UserService.GetRolesLookup();
                _cbRole.DisplayMember = "Name";
                _cbRole.ValueMember = "Id";
                _cbRole.DataSource = roles;

                var emps = EmployeeService.GetLookupActive();
                var dt = emps.Copy();
                var nr = dt.NewRow();
                nr["Id"] = -1;
                nr["DisplayName"] = "(не привязан)";
                dt.Rows.InsertAt(nr, 0);
                _cbEmp.DisplayMember = "DisplayName";
                _cbEmp.ValueMember = "Id";
                _cbEmp.DataSource = dt;

                if (!_id.HasValue)
                {
                    _cbEmp.SelectedValue = -1;
                    return;
                }

                const string sql = @"
SELECT LoginName, FullName, RoleId, EmployeeId, IsActive
FROM dbo.Users WHERE Id = @Id AND IsDeleted = 0;";
                var t = Db.ExecuteDataTable(sql, new SqlParameter("@Id", _id.Value));
                if (t.Rows.Count == 0)
                {
                    MessageBox.Show(this, "Запись не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                var row = t.Rows[0];
                _txtLogin.Text = row["LoginName"].ToString();
                _txtName.Text = row["FullName"].ToString();
                _cbRole.SelectedValue = Convert.ToInt32(row["RoleId"]);
                if (row["EmployeeId"] == DBNull.Value)
                {
                    _cbEmp.SelectedValue = -1;
                }
                else
                {
                    _cbEmp.SelectedValue = Convert.ToInt32(row["EmployeeId"]);
                }

                _chkActive.Checked = Convert.ToBoolean(row["IsActive"]);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("UserEdit.Load", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var login = _txtLogin.Text.Trim();
            if (!InputValidators.IsValidLogin(login))
            {
                MessageBox.Show(this, "Логин: 3..64 символа, без пробелов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var name = _txtName.Text.Trim();
            if (name.Length < 3)
            {
                MessageBox.Show(this, "ФИО не короче 3 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var pwd = _txtPassword.Text;
            if (!_id.HasValue && (pwd.Length < 6 || pwd.Length > 128))
            {
                MessageBox.Show(this, "Пароль при создании: 6..128 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_id.HasValue && !string.IsNullOrWhiteSpace(pwd) && (pwd.Length < 6 || pwd.Length > 128))
            {
                MessageBox.Show(this, "Новый пароль должен быть 6..128 символов или оставьте поле пустым.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cbRole.SelectedValue == null)
            {
                MessageBox.Show(this, "Выберите роль.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var role = Convert.ToInt32(_cbRole.SelectedValue);
            int? emp = null;
            if (_cbEmp.SelectedValue != null && Convert.ToInt32(_cbEmp.SelectedValue) > 0)
            {
                emp = Convert.ToInt32(_cbEmp.SelectedValue);
            }

            try
            {
                if (_id.HasValue)
                {
                    var newPwd = string.IsNullOrWhiteSpace(pwd) ? null : pwd;
                    UserService.Update(_id.Value, login, name, role, emp, _chkActive.Checked, newPwd);
                    AuditService.LogChange("Users", "UPDATE", _id.Value.ToString(), null, login);
                }
                else
                {
                    UserService.Insert(login, pwd, name, role, emp, _chkActive.Checked);
                    AuditService.LogChange("Users", "INSERT", null, null, login);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show(this, "Логин должен быть уникальным.", "Уникальность", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("UserEdit.Save", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
