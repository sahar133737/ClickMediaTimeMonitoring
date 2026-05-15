using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using ClickMediaWorkTime.Infrastructure;
using ClickMediaWorkTime.Security;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class WorkTimeEntryEditForm : Form
    {
        private readonly long? _id;
        private readonly ComboBox _cbEmployee;
        private readonly ComboBox _cbProject;
        private readonly DateTimePicker _dtDay;
        private readonly DateTimePicker _dtStart;
        private readonly DateTimePicker _dtEnd;
        private readonly NumericUpDown _numBreak;
        private readonly ComboBox _cbType;
        private readonly TextBox _txtComment;

        public WorkTimeEntryEditForm(long? id)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, id.HasValue ? "Запись учёта времени — редактирование" : "Запись учёта времени — создание");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 580;
            Height = 580;
            StartPosition = FormStartPosition.CenterParent;

            int y = 10;
            Controls.Add(ThemeHelper.FormFieldLabel("Сотрудник *:", 18, y, 220));
            _cbEmployee = new ComboBox { Left = 18, Top = y + 22, Width = 520, DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, MaxDropDownItems = 15 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Проект *:", 18, y, 200));
            _cbProject = new ComboBox { Left = 18, Top = y + 22, Width = 520, DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, MaxDropDownItems = 15 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Дата работы *:", 18, y, 200));
            _dtDay = new DateTimePicker { Left = 18, Top = y + 22, Width = 200, Format = DateTimePickerFormat.Short, MaxDate = DateTime.Today.AddDays(1) };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Начало *:", 18, y, 200));
            _dtStart = new DateTimePicker
            {
                Left = 18,
                Top = y + 22,
                Width = 120,
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true
            };
            Controls.Add(ThemeHelper.FormFieldLabel("Окончание *:", 220, y, 200));
            _dtEnd = new DateTimePicker
            {
                Left = 220,
                Top = y + 22,
                Width = 120,
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true
            };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Перерыв, мин *:", 18, y, 220));
            _numBreak = new NumericUpDown { Left = 18, Top = y + 22, Width = 120, Minimum = 0, Maximum = 300, Increment = 5 };
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Тип учёта *:", 18, y, 200));
            _cbType = new ComboBox { Left = 18, Top = y + 22, Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnNewType = new Button { Left = 408, Top = y + 18, Width = 130, Height = 32, Text = "Новый тип…" };
            ThemeHelper.StyleSecondary(btnNewType);
            btnNewType.Click += BtnNewType_Click;
            y += 62;
            Controls.Add(ThemeHelper.FormFieldLabel("Комментарий:", 18, y, 200));
            _txtComment = new TextBox { Left = 18, Top = y + 22, Width = 520, Height = 70, Multiline = true, MaxLength = 500, ScrollBars = ScrollBars.Vertical };

            var btnOk = new Button { Left = 18, Top = y + 100, Width = 170, Height = 36, Text = "Сохранить" };
            var btnCancel = new Button { Left = 200, Top = y + 100, Width = 170, Height = 36, Text = "Отмена", DialogResult = DialogResult.Cancel };
            ThemeHelper.StyleButton(btnOk, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnOk.Click += BtnOk_Click;

            Controls.AddRange(new Control[] { _cbEmployee, _cbProject, _dtDay, _dtStart, _dtEnd, _numBreak, _cbType, btnNewType, _txtComment, btnOk, btnCancel });
            CancelButton = btnCancel;
            Load += WorkTimeEntryEditForm_Load;
        }

        private void WorkTimeEntryEditForm_Load(object sender, EventArgs e)
        {
            try
            {
                var emps = EmployeeService.GetLookupActive();
                _cbEmployee.DisplayMember = "DisplayName";
                _cbEmployee.ValueMember = "Id";
                _cbEmployee.DataSource = emps;

                var projs = ProjectService.GetLookupActive();
                _cbProject.DisplayMember = "DisplayName";
                _cbProject.ValueMember = "Id";
                _cbProject.DataSource = projs;

                ReloadTypesCombo(null);

                var isEmp = string.Equals(CurrentUserContext.RoleCode, "EMPLOYEE", StringComparison.OrdinalIgnoreCase);
                if (isEmp && CurrentUserContext.EmployeeId.HasValue)
                {
                    _cbEmployee.Enabled = false;
                    _cbEmployee.SelectedValue = CurrentUserContext.EmployeeId.Value;
                }

                _dtStart.Value = DateTime.Today.AddHours(9);
                _dtEnd.Value = DateTime.Today.AddHours(18);

                if (WorkTimeEntriesForm.LastFilterProjectId.HasValue && _cbProject.Items.Count > 0)
                {
                    try
                    {
                        _cbProject.SelectedValue = WorkTimeEntriesForm.LastFilterProjectId.Value;
                    }
                    catch
                    {
                        // ignore invalid selection
                    }
                }

                if (WorkTimeEntriesForm.LastFilterWorkDate.HasValue)
                {
                    _dtDay.Value = WorkTimeEntriesForm.LastFilterWorkDate.Value;
                }

                if (!_id.HasValue)
                {
                    return;
                }

                const string sql = @"
SELECT EmployeeId, ProjectId, WorkDate, StartTime, EndTime, BreakMinutes, TypeId, Comment
FROM dbo.WorkTimeEntries WHERE Id = @Id;";
                var t = Db.ExecuteDataTable(sql, new SqlParameter("@Id", _id.Value));
                if (t.Rows.Count == 0)
                {
                    MessageBox.Show(this, "Запись не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                var row = t.Rows[0];
                _cbEmployee.SelectedValue = Convert.ToInt32(row["EmployeeId"]);
                _cbProject.SelectedValue = Convert.ToInt32(row["ProjectId"]);
                _dtDay.Value = Convert.ToDateTime(row["WorkDate"]);
                var start = row["StartTime"] is TimeSpan ts1 ? ts1 : Convert.ToDateTime(row["StartTime"]).TimeOfDay;
                var end = row["EndTime"] is TimeSpan ts2 ? ts2 : Convert.ToDateTime(row["EndTime"]).TimeOfDay;
                _dtStart.Value = DateTime.Today.Add(start);
                _dtEnd.Value = DateTime.Today.Add(end);
                _numBreak.Value = Convert.ToInt32(row["BreakMinutes"]);
                ReloadTypesCombo(Convert.ToInt32(row["TypeId"]));
                _txtComment.Text = row["Comment"] == DBNull.Value ? string.Empty : row["Comment"].ToString();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("WorkTimeEntryEdit.Load", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_cbEmployee.SelectedValue == null || _cbProject.SelectedValue == null || _cbType.SelectedValue == null)
            {
                MessageBox.Show(this, "Заполните обязательные поля и списки выбора.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var emp = Convert.ToInt32(_cbEmployee.SelectedValue);
            var proj = Convert.ToInt32(_cbProject.SelectedValue);
            var type = Convert.ToInt32(_cbType.SelectedValue);
            var day = _dtDay.Value.Date;
            var start = _dtStart.Value.TimeOfDay;
            var end = _dtEnd.Value.TimeOfDay;
            var br = (int)_numBreak.Value;

            try
            {
                if (_id.HasValue)
                {
                    WorkTimeEntryService.Update(_id.Value, emp, proj, day, start, end, br, type, _txtComment.Text.Trim());
                    AuditService.LogChange("WorkTimeEntries", "UPDATE", _id.Value.ToString(), null, $"Emp={emp};Proj={proj}");
                }
                else
                {
                    WorkTimeEntryService.Insert(emp, proj, day, start, end, br, type, _txtComment.Text.Trim());
                    AuditService.LogChange("WorkTimeEntries", "INSERT", null, null, $"Emp={emp};Proj={proj}");
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("WorkTimeEntryEdit.Save", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ReloadTypesCombo(int? selectId)
        {
            var types = WorkTimeEntryService.GetTypes();
            _cbType.DisplayMember = "Name";
            _cbType.ValueMember = "Id";
            _cbType.DataSource = types;
            if (selectId.HasValue && types.Rows.Count > 0)
            {
                try
                {
                    _cbType.SelectedValue = selectId.Value;
                }
                catch
                {
                    if (types.Rows.Count > 0)
                    {
                        _cbType.SelectedIndex = 0;
                    }
                }
            }
            else if (types.Rows.Count > 0)
            {
                _cbType.SelectedIndex = 0;
            }
        }

        private void BtnNewType_Click(object sender, EventArgs e)
        {
            try
            {
                var name = PromptText(this, "Новый тип", "Введите название нового типа учёта времени:");
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                var id = WorkTimeEntryService.InsertType(name.Trim());
                ReloadTypesCombo(id);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, ex.Message, "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("WorkTimeEntryEdit.NewType", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string PromptText(IWin32Window owner, string caption, string message)
        {
            using (var f = new Form())
            {
                f.Text = caption;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MinimizeBox = false;
                f.MaximizeBox = false;
                f.Width = 440;
                f.Height = 160;
                var lbl = new Label { Left = 14, Top = 14, Width = 400, Text = message };
                var tb = new TextBox { Left = 14, Top = 38, Width = 400, MaxLength = 120 };
                var ok = new Button { Text = "OK", Left = 230, Top = 78, Width = 90, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Отмена", Left = 324, Top = 78, Width = 90, DialogResult = DialogResult.Cancel };
                f.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                return f.ShowDialog(owner) == DialogResult.OK ? tb.Text.Trim() : null;
            }
        }
    }
}
