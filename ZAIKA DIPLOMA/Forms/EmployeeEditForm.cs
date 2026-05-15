using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class EmployeeEditForm : Form
    {
        private readonly int? _id;
        private readonly ComboBox _cbDept;
        private readonly ComboBox _cbPos;
        private readonly TextBox _txtName;
        private readonly TextBox _txtNum;
        private readonly MaskedTextBox _txtPhone;
        private readonly TextBox _txtEmail;
        private readonly DateTimePicker _dtHire;
        private readonly CheckBox _chkActive;

        public EmployeeEditForm(int? id)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, id.HasValue ? "Сотрудник — редактирование" : "Сотрудник — создание");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 560;
            Height = 540;
            StartPosition = FormStartPosition.CenterParent;

            int y = 14;
            Controls.Add(ThemeHelper.FormFieldLabel("Подразделение *:", 18, y, 200));
            _cbDept = new ComboBox { Left = 18, Top = y + 22, Width = 500, DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, MaxDropDownItems = 12 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Должность *:", 18, y, 200));
            _cbPos = new ComboBox { Left = 18, Top = y + 22, Width = 500, DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, MaxDropDownItems = 12 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("ФИО *:", 18, y, 200));
            _txtName = new TextBox { Left = 18, Top = y + 22, Width = 500, MaxLength = 200 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Табельный номер *:", 18, y, 220));
            _txtNum = new TextBox { Left = 18, Top = y + 22, Width = 240, MaxLength = 40 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Телефон:", 18, y, 200));
            _txtPhone = new MaskedTextBox { Left = 18, Top = y + 22, Width = 240, Mask = "+7 (000) 000-00-00", PromptChar = '_' };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("E-mail:", 18, y, 200));
            _txtEmail = new TextBox { Left = 18, Top = y + 22, Width = 500, MaxLength = 200 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Дата приёма *:", 18, y, 200));
            _dtHire = new DateTimePicker { Left = 18, Top = y + 22, Width = 200, Format = DateTimePickerFormat.Short, MaxDate = DateTime.Today.AddDays(1) };
            _chkActive = new CheckBox { Left = 240, Top = y + 22, Width = 200, Text = "Активен", Checked = true };
            y += 62;

            var linkDept = new LinkLabel { Left = 18, Top = y, Width = 240, Text = "Добавить подразделение…" };
            linkDept.Click += LinkDept_Click;
            var linkPos = new LinkLabel { Left = 270, Top = y, Width = 240, Text = "Добавить должность…" };
            linkPos.Click += LinkPos_Click;
            y += 28;

            var btnOk = new Button { Left = 18, Top = y + 8, Width = 150, Height = 36, Text = "Сохранить" };
            var btnCancel = new Button { Left = 180, Top = y + 8, Width = 150, Height = 36, Text = "Отмена", DialogResult = DialogResult.Cancel };
            ThemeHelper.StyleButton(btnOk, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnOk.Click += BtnOk_Click;

            Controls.AddRange(new Control[] { _cbDept, _cbPos, _txtName, _txtNum, _txtPhone, _txtEmail, _dtHire, _chkActive, linkDept, linkPos, btnOk, btnCancel });
            CancelButton = btnCancel;
            Load += EmployeeEditForm_Load;
        }

        private void EmployeeEditForm_Load(object sender, EventArgs e)
        {
            try
            {
                var deps = DepartmentService.GetAll();
                _cbDept.DisplayMember = "Name";
                _cbDept.ValueMember = "Id";
                _cbDept.DataSource = deps;

                var pos = PositionService.GetAll();
                _cbPos.DisplayMember = "Name";
                _cbPos.ValueMember = "Id";
                _cbPos.DataSource = pos;

                if (!_id.HasValue)
                {
                    return;
                }

                var t = Db.ExecuteDataTable(
                    @"SELECT DepartmentId, PositionId, FullName, PersonnelNumber, Phone, Email, HireDate, IsActive
                      FROM dbo.Employees WHERE Id = @Id AND IsDeleted = 0;",
                    new SqlParameter("@Id", _id.Value));
                if (t.Rows.Count == 0)
                {
                    MessageBox.Show(this, "Запись не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                var row = t.Rows[0];
                _cbDept.SelectedValue = Convert.ToInt32(row["DepartmentId"]);
                _cbPos.SelectedValue = Convert.ToInt32(row["PositionId"]);
                _txtName.Text = row["FullName"].ToString();
                _txtNum.Text = row["PersonnelNumber"].ToString();
                if (row["Phone"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["Phone"].ToString()))
                {
                    _txtPhone.Text = row["Phone"].ToString();
                }

                _txtEmail.Text = row["Email"] == DBNull.Value ? string.Empty : row["Email"].ToString();
                _dtHire.Value = Convert.ToDateTime(row["HireDate"]);
                _chkActive.Checked = Convert.ToBoolean(row["IsActive"]);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("EmployeeEdit.Load", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_cbDept.SelectedValue == null || _cbPos.SelectedValue == null)
            {
                MessageBox.Show(this, "Заполните справочники подразделения и должности.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var name = _txtName.Text.Trim();
            if (name.Length < 3)
            {
                MessageBox.Show(this, "ФИО не короче 3 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var num = _txtNum.Text.Trim();
            if (!InputValidators.IsValidPersonnelNumber(num))
            {
                MessageBox.Show(this, "Табельный номер: 3..40 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var email = _txtEmail.Text.Trim();
            if (!InputValidators.IsValidEmail(email))
            {
                MessageBox.Show(this, "Некорректный e-mail.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string phone = null;
            var raw = _txtPhone.Text ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(raw.Replace("_", string.Empty)))
            {
                if (!_txtPhone.MaskFull)
                {
                    MessageBox.Show(this, "Заполните телефон полностью по маске или очистите поле.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                phone = _txtPhone.Text;
            }

            try
            {
                var dept = Convert.ToInt32(_cbDept.SelectedValue);
                var pos = Convert.ToInt32(_cbPos.SelectedValue);
                if (_id.HasValue)
                {
                    EmployeeService.Update(_id.Value, dept, pos, name, num, phone, string.IsNullOrWhiteSpace(email) ? null : email, _dtHire.Value, _chkActive.Checked);
                    AuditService.LogChange("Employees", "UPDATE", _id.Value.ToString(), null, name);
                }
                else
                {
                    EmployeeService.Insert(dept, pos, name, num, phone, string.IsNullOrWhiteSpace(email) ? null : email, _dtHire.Value, _chkActive.Checked);
                    AuditService.LogChange("Employees", "INSERT", null, null, name);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show(this, "Табельный номер должен быть уникальным.", "Уникальность", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("EmployeeEdit.Save", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkDept_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new DepartmentEditForm(null))
                {
                    if (f.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                }

                var deps = DepartmentService.GetAll();
                _cbDept.DisplayMember = "Name";
                _cbDept.ValueMember = "Id";
                _cbDept.DataSource = deps;
                if (_cbDept.Items.Count > 0)
                {
                    _cbDept.SelectedIndex = _cbDept.Items.Count - 1;
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("EmployeeEdit.LinkDept", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkPos_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new PositionEditForm(null))
                {
                    if (f.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                }

                var pos = PositionService.GetAll();
                _cbPos.DisplayMember = "Name";
                _cbPos.ValueMember = "Id";
                _cbPos.DataSource = pos;
                if (_cbPos.Items.Count > 0)
                {
                    _cbPos.SelectedIndex = _cbPos.Items.Count - 1;
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("EmployeeEdit.LinkPos", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
