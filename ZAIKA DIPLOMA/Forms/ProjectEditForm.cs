using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class ProjectEditForm : Form
    {
        private readonly int? _id;
        private readonly TextBox _txtName;
        private readonly TextBox _txtClient;
        private readonly ComboBox _cbStatus;
        private readonly DateTimePicker _dtStart;
        private readonly DateTimePicker _dtEnd;
        private readonly CheckBox _chkHasEnd;
        private readonly NumericUpDown _numHours;

        public ProjectEditForm(int? id)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, id.HasValue ? "Проект — редактирование" : "Проект — создание");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 560;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;

            int y = 12;
            Controls.Add(ThemeHelper.FormFieldLabel("Название *:", 18, y, 200));
            _txtName = new TextBox { Left = 18, Top = y + 22, Width = 500, MaxLength = 250 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Клиент:", 18, y, 200));
            _txtClient = new TextBox { Left = 18, Top = y + 22, Width = 500, MaxLength = 250 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Статус *:", 18, y, 200));
            _cbStatus = new ComboBox { Left = 18, Top = y + 22, Width = 500, DropDownStyle = ComboBoxStyle.DropDownList };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Дата начала *:", 18, y, 220));
            _dtStart = new DateTimePicker { Left = 18, Top = y + 22, Width = 200, Format = DateTimePickerFormat.Short };
            y += 62;
            _chkHasEnd = new CheckBox { Left = 18, Top = y, Width = 260, Text = "Указать дату окончания" };
            _dtEnd = new DateTimePicker { Left = 18, Top = y + 26, Width = 200, Format = DateTimePickerFormat.Short, Enabled = false };
            _chkHasEnd.CheckedChanged += (s, e) => _dtEnd.Enabled = _chkHasEnd.Checked;
            y += 70;
            Controls.Add(ThemeHelper.FormFieldLabel("План часов *:", 18, y, 200));
            _numHours = new NumericUpDown { Left = 18, Top = y + 22, Width = 200, Minimum = 0, Maximum = 100000, DecimalPlaces = 2 };

            var btnOk = new Button { Left = 18, Top = y + 70, Width = 150, Height = 34, Text = "Сохранить" };
            var btnCancel = new Button { Left = 180, Top = y + 70, Width = 150, Height = 34, Text = "Отмена", DialogResult = DialogResult.Cancel };
            ThemeHelper.StyleButton(btnOk, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnOk.Click += BtnOk_Click;

            Controls.AddRange(new Control[]
            {
                _txtName, _txtClient, _cbStatus, _dtStart, _chkHasEnd, _dtEnd, _numHours, btnOk, btnCancel
            });
            CancelButton = btnCancel;
            Load += ProjectEditForm_Load;
        }

        private void ProjectEditForm_Load(object sender, EventArgs e)
        {
            try
            {
                var st = ProjectService.GetStatuses();
                _cbStatus.DisplayMember = "Name";
                _cbStatus.ValueMember = "Id";
                _cbStatus.DataSource = st;

                if (!_id.HasValue)
                {
                    _dtStart.Value = DateTime.Today;
                    _numHours.Value = 40;
                    return;
                }

                const string sql = @"
SELECT Name, ClientName, StatusId, StartDate, EndDate, PlannedHours
FROM dbo.Projects WHERE Id = @Id AND IsDeleted = 0;";
                var t = Db.ExecuteDataTable(sql, new SqlParameter("@Id", _id.Value));
                if (t.Rows.Count == 0)
                {
                    MessageBox.Show(this, "Запись не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                var row = t.Rows[0];
                _txtName.Text = row["Name"].ToString();
                _txtClient.Text = row["ClientName"] == DBNull.Value ? string.Empty : row["ClientName"].ToString();
                _cbStatus.SelectedValue = Convert.ToInt32(row["StatusId"]);
                _dtStart.Value = Convert.ToDateTime(row["StartDate"]);
                if (row["EndDate"] != DBNull.Value)
                {
                    _chkHasEnd.Checked = true;
                    _dtEnd.Value = Convert.ToDateTime(row["EndDate"]);
                }

                _numHours.Value = Math.Max(0, Convert.ToDecimal(row["PlannedHours"]));
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("ProjectEdit.Load", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var name = _txtName.Text.Trim();
            if (name.Length < 2)
            {
                MessageBox.Show(this, "Название проекта не короче 2 символов.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cbStatus.SelectedValue == null)
            {
                MessageBox.Show(this, "Выберите статус.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime? end = _chkHasEnd.Checked ? _dtEnd.Value.Date : (DateTime?)null;
            if (end.HasValue && end < _dtStart.Value.Date)
            {
                MessageBox.Show(this, "Дата окончания не может быть раньше даты начала.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var status = Convert.ToInt32(_cbStatus.SelectedValue);
                var client = string.IsNullOrWhiteSpace(_txtClient.Text) ? null : _txtClient.Text.Trim();
                if (_id.HasValue)
                {
                    ProjectService.Update(_id.Value, name, client, status, _dtStart.Value, end, _numHours.Value);
                    AuditService.LogChange("Projects", "UPDATE", _id.Value.ToString(), null, name);
                }
                else
                {
                    ProjectService.Insert(name, client, status, _dtStart.Value, end, _numHours.Value);
                    AuditService.LogChange("Projects", "INSERT", null, null, name);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("ProjectEdit.Save", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
