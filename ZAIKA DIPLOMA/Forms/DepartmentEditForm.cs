using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class DepartmentEditForm : Form
    {
        private readonly int? _id;
        private readonly TextBox _txtName;
        private readonly TextBox _txtDesc;

        public DepartmentEditForm(int? id)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, id.HasValue ? "Редактирование подразделения" : "Новое подразделение");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 520;
            Height = 300;
            StartPosition = FormStartPosition.CenterParent;

            var lblName = ThemeHelper.FormFieldLabel("Название *:", 18, 18, 200);
            _txtName = new TextBox { Left = 18, Top = 40, Width = 460, MaxLength = 200 };

            var lblDesc = ThemeHelper.FormFieldLabel("Описание:", 18, 78, 200);
            _txtDesc = new TextBox { Left = 18, Top = 100, Width = 460, Height = 90, Multiline = true, ScrollBars = ScrollBars.Vertical, MaxLength = 500 };

            var btnOk = new Button { Left = 18, Top = 210, Width = 140, Height = 36, Text = "Сохранить", DialogResult = DialogResult.None };
            var btnCancel = new Button { Left = 170, Top = 210, Width = 140, Height = 36, Text = "Отмена", DialogResult = DialogResult.Cancel };
            ThemeHelper.StyleButton(btnOk, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnOk.Click += BtnOk_Click;

            Controls.AddRange(new Control[] { lblName, _txtName, lblDesc, _txtDesc, btnOk, btnCancel });
            CancelButton = btnCancel;
            Load += DepartmentEditForm_Load;
        }

        private void DepartmentEditForm_Load(object sender, EventArgs e)
        {
            if (!_id.HasValue)
            {
                return;
            }

            try
            {
                const string sql = "SELECT Name, Description FROM dbo.Departments WHERE Id = @Id AND IsDeleted = 0;";
                var t = Db.ExecuteDataTable(sql, new SqlParameter("@Id", _id.Value));
                if (t.Rows.Count == 0)
                {
                    MessageBox.Show(this, "Запись не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                _txtName.Text = t.Rows[0]["Name"].ToString();
                _txtDesc.Text = t.Rows[0]["Description"] == DBNull.Value ? string.Empty : t.Rows[0]["Description"].ToString();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("DepartmentEdit.Load", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var name = _txtName.Text.Trim();
            if (name.Length < 2)
            {
                MessageBox.Show(this, "Название не короче 2 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_id.HasValue)
                {
                    DepartmentService.Update(_id.Value, name, _txtDesc.Text.Trim());
                    AuditService.LogChange("Departments", "UPDATE", _id.Value.ToString(), null, name);
                }
                else
                {
                    DepartmentService.Insert(name, _txtDesc.Text.Trim());
                    AuditService.LogChange("Departments", "INSERT", null, null, name);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show(this, "Подразделение с таким названием уже существует.", "Уникальность", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("DepartmentEdit.Save", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
