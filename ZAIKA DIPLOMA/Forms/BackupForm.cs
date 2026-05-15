using System;
using System.Data;
using System.Windows.Forms;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class BackupForm : Form
    {
        private const string HelpKey = "backups";

        private readonly DataGridView _grid;
        private readonly TextBox _txtSearch;
        private readonly ComboBox _cbKind;
        private readonly CheckBox _chkAutoOnExit;
        private DataTable _table;
        private DataView _view;

        public BackupForm()
        {
            ThemeHelper.ApplyForm(this, "Клик Медиа — Резервные копии");
            Width = 1240;
            Height = 760;
            if (!RolePermissionService.HasPermission("module.backups"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа.", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = ThemeHelper.CreateCardGroup("Операции и фильтр", 228);
            var lblHint = new Label
            {
                Left = 14,
                Top = 18,
                Width = 1180,
                Height = 20,
                ForeColor = ThemeHelper.MutedText,
                Font = ThemeHelper.UiFont,
                Text = "Каталог .bak должен быть доступен учётной записи службы SQL Server."
            };
            _chkAutoOnExit = new CheckBox
            {
                Left = 14,
                Top = 42,
                Width = 780,
                Height = 22,
                Text = "Автоматически создавать резервную копию при закрытии главного окна приложения"
            };
            _chkAutoOnExit.Checked = AppPreferences.AutoBackupOnExit;
            _chkAutoOnExit.CheckedChanged += (s, e) =>
            {
                AppPreferences.AutoBackupOnExit = _chkAutoOnExit.Checked;
            };

            var btnCreate = new Button { Left = 14, Top = 72, Width = 240, Height = 34, Text = "Создать .bak (выбрать файл)…" };
            var btnQuick = new Button { Left = 262, Top = 72, Width = 280, Height = 34, Text = "Быстрый бэкап в рекомендуемую папку" };
            var btnRestore = new Button { Left = 550, Top = 72, Width = 220, Height = 34, Text = "Восстановить из .bak…" };
            var btnRefresh = new Button { Left = 778, Top = 72, Width = 150, Height = 34, Text = "Обновить" };
            var btnHelp = new Button { Left = 936, Top = 72, Width = 150, Height = 34, Text = "Справка (F11)" };
            var btnClose = new Button { Left = 1094, Top = 72, Width = 120, Height = 34, Text = "Закрыть" };
            ThemeHelper.StylePrimary(btnCreate);
            ThemeHelper.StyleSecondary(btnQuick);
            ThemeHelper.StyleDanger(btnRestore);
            ThemeHelper.StyleSecondary(btnRefresh);
            ThemeHelper.StyleAccentOutline(btnHelp);
            ThemeHelper.StyleButton(btnClose, ThemeHelper.Accent);
            btnCreate.Click += BtnCreate_Click;
            btnQuick.Click += BtnQuick_Click;
            btnRestore.Click += BtnRestore_Click;
            btnRefresh.Click += (s, e) => Reload();
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp(HelpKey, this);
            btnClose.Click += (s, e) => Close();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Тип копии:", 14, 118, 120));
            _cbKind = new ComboBox { Left = 14, Top = 140, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbKind.Items.AddRange(new object[] { "Все", "Только автоматические", "Только ручные" });
            _cbKind.SelectedIndex = 0;
            _cbKind.SelectedIndexChanged += (s, e) => Reload();

            top.Controls.Add(ThemeHelper.FormFieldLabel("Поиск:", 250, 118, 120));
            _txtSearch = new TextBox { Left = 250, Top = 140, Width = 360 };
            var btnFind = new Button { Left = 618, Top = 136, Width = 110, Height = 32, Text = "Найти" };
            var btnReset = new Button { Left = 734, Top = 136, Width = 110, Height = 32, Text = "Сброс" };
            ThemeHelper.StylePrimary(btnFind);
            ThemeHelper.StyleSecondary(btnReset);
            btnFind.Click += (s, e) => Reload();
            btnReset.Click += (s, e) =>
            {
                _txtSearch.Clear();
                _cbKind.SelectedIndex = 0;
                Reload();
            };

            top.Controls.AddRange(new Control[] { lblHint, _chkAutoOnExit, btnCreate, btnQuick, btnRestore, btnRefresh, btnHelp, btnClose, _cbKind, _txtSearch, btnFind, btnReset });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            ThemeHelper.StyleGrid(_grid);
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
                _table = BackupService.GetFiltered(_txtSearch.Text, _cbKind.SelectedIndex);
                _view = GridPresentation.Bind(_grid, _table, "backups");
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("BackupForm.Reload", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "Резервная копия (*.bak)|*.bak";
                    dlg.InitialDirectory = BackupService.GetRecommendedBackupDirectory();
                    dlg.FileName = $"ClickMediaTimeDB_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    BackupService.CreateBackupToFile(dlg.FileName, "Ручное резервное копирование", false);
                    MessageBox.Show(this, "Копия создана:\n" + dlg.FileName, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Reload();
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "Резервное копирование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("BackupForm.Create", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnQuick_Click(object sender, EventArgs e)
        {
            try
            {
                var path = BackupService.GetDefaultBackupFilePath($"ClickMediaTimeDB_quick_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                BackupService.CreateBackupToFile(path, "Быстрое резервное копирование", false);
                MessageBox.Show(this, "Копия создана:\n" + path, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Reload();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "Резервное копирование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("BackupForm.Quick", ex);
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "Файлы резервных копий (*.bak)|*.bak";
                    dlg.InitialDirectory = BackupService.GetRecommendedBackupDirectory();
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    if (MessageBox.Show(this, "Восстановление перезапишет текущую базу. Продолжить?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        return;
                    }

                    BackupService.RestoreBackup(dlg.FileName);
                    MessageBox.Show(this, "Восстановление завершено. Перезапустите приложение.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("BackupForm.Restore", ex);
                MessageBox.Show(this, ex.Message, "Ошибка восстановления", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
