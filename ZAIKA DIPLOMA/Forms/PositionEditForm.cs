using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class PositionEditForm : Form
    {
        private readonly int? _id;
        private readonly TextBox _txtName;

        public PositionEditForm(int? id)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, id.HasValue ? "Редактирование должности" : "Новая должность");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 480;
            Height = 220;
            StartPosition = FormStartPosition.CenterParent;

            Controls.Add(ThemeHelper.FormFieldLabel("Название *:", 18, 18, 200));
            _txtName = new TextBox { Left = 18, Top = 40, Width = 420, MaxLength = 200 };

            var btnOk = new Button { Left = 18, Top = 100, Width = 140, Height = 34, Text = "Сохранить" };
            var btnCancel = new Button { Left = 170, Top = 100, Width = 140, Height = 34, Text = "Отмена", DialogResult = DialogResult.Cancel };
            ThemeHelper.StyleButton(btnOk, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnOk.Click += BtnOk_Click;
            Controls.Add(_txtName);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
            Load += PositionEditForm_Load;
        }

        private void PositionEditForm_Load(object sender, EventArgs e)
        {
            if (!_id.HasValue)
            {
                return;
            }

            try
            {
                var t = Db.ExecuteDataTable("SELECT Name FROM dbo.Positions WHERE Id = @Id AND IsDeleted = 0;", new SqlParameter("@Id", _id.Value));
                if (t.Rows.Count == 0)
                {
                    MessageBox.Show(this, "Запись не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                _txtName.Text = t.Rows[0]["Name"].ToString();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("PositionEdit.Load", ex);
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
                    PositionService.Update(_id.Value, name);
                    AuditService.LogChange("Positions", "UPDATE", _id.Value.ToString(), null, name);
                }
                else
                {
                    PositionService.Insert(name);
                    AuditService.LogChange("Positions", "INSERT", null, null, name);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show(this, "Такая должность уже существует.", "Уникальность", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("PositionEdit.Save", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
