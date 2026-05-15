using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ClickMediaWorkTime.Services;
using ClickMediaWorkTime.UI;

namespace ClickMediaWorkTime.Forms
{
    public sealed class LoginForm : Form
    {
        private readonly ComboBox _cbLogin;
        private readonly TextBox _txtPassword;
        private readonly Button _btnLogin;
        private readonly Label _lblError;
        private readonly ToolTip _tip;
        private readonly LinkLabel _lnkRemember;
        private static readonly string LastLoginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last-login-clickmedia.txt");

        public LoginForm()
        {
            Text = "Клик Медиа — вход";
            MinimumSize = new Size(680, 560);
            Size = new Size(760, 600);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ThemeHelper.ShellBg;
            Font = ThemeHelper.UiFont;
            Padding = new Padding(0);
            _tip = new ToolTip { ShowAlways = false, InitialDelay = 120, AutoPopDelay = 2800 };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ThemeHelper.ShellBg
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 228));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var leftBrand = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = ThemeHelper.ShellPanel };
            leftBrand.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(ThemeHelper.Brand))
                {
                    e.Graphics.FillRectangle(b, 0, 0, 5, leftBrand.Height);
                }
            };

            var brand = new Label
            {
                Dock = DockStyle.Top,
                Height = 132,
                ForeColor = ThemeHelper.Mint,
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                Text = "КЛИК" + Environment.NewLine + "МЕДИА",
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 44, 8, 0)
            };
            var sub = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(190, 198, 220),
                Font = ThemeHelper.UiFont,
                Text = "Учёт времени" + Environment.NewLine + "и проектов",
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(8, 8, 8, 0)
            };
            leftBrand.Controls.Add(sub);
            leftBrand.Controls.Add(brand);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = ThemeHelper.ShellBg, Padding = new Padding(24, 32, 24, 32) };
            var centerHost = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = ThemeHelper.ShellBg
            };
            centerHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            centerHost.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 452));
            centerHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var card = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeHelper.Card,
                BorderColor = Color.FromArgb(218, 214, 208),
                Radius = 18,
                Padding = new Padding(32, 30, 32, 26)
            };

            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                BackColor = Color.Transparent
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            var lblTitle = new Label
            {
                Text = "Вход в систему",
                Font = ThemeHelper.HeroFont,
                ForeColor = ThemeHelper.Text,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Color.Transparent
            };
            inner.SetColumnSpan(lblTitle, 2);
            inner.Controls.Add(lblTitle, 0, 0);

            var lblLogin = ThemeHelper.FormFieldLabel("Логин", 0, 0, 100);
            lblLogin.Dock = DockStyle.Fill;
            lblLogin.TextAlign = ContentAlignment.MiddleLeft;
            inner.Controls.Add(lblLogin, 0, 1);

            _cbLogin = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                FlatStyle = FlatStyle.Standard,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                MaxLength = 64,
                DropDownHeight = 220,
                Margin = new Padding(0, 2, 0, 0)
            };
            _cbLogin.Leave += (s, e) => SanitizeLogin();
            inner.Controls.Add(_cbLogin, 1, 1);

            var lblPwd = ThemeHelper.FormFieldLabel("Пароль", 0, 0, 100);
            lblPwd.Dock = DockStyle.Fill;
            lblPwd.TextAlign = ContentAlignment.MiddleLeft;
            inner.Controls.Add(lblPwd, 0, 2);

            _txtPassword = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true,
                MaxLength = 128,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 0, 0)
            };
            inner.Controls.Add(_txtPassword, 1, 2);

            _btnLogin = new Button { Text = "Войти", Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
            ThemeHelper.StylePrimary(_btnLogin);
            _btnLogin.Click += BtnLogin_Click;
            inner.SetColumnSpan(_btnLogin, 2);
            inner.Controls.Add(_btnLogin, 0, 3);

            _lblError = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = ThemeHelper.Danger,
                Font = ThemeHelper.UiFont,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 0)
            };
            inner.SetColumnSpan(_lblError, 2);
            inner.Controls.Add(_lblError, 0, 4);

            _lnkRemember = new LinkLabel
            {
                Text = "Сохранить логин в списке на этом ПК",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                LinkColor = ThemeHelper.Brand,
                ActiveLinkColor = ControlPaint.Dark(ThemeHelper.Brand, 0.15f),
                VisitedLinkColor = ThemeHelper.Brand,
                DisabledLinkColor = ThemeHelper.MutedText,
                Margin = new Padding(0, 4, 0, 0),
                AutoSize = false
            };
            _lnkRemember.LinkClicked += LnkRemember_LinkClicked;
            inner.SetColumnSpan(_lnkRemember, 2);
            inner.Controls.Add(_lnkRemember, 0, 5);

            card.Controls.Add(inner);
            centerHost.Controls.Add(card, 1, 0);
            right.Controls.Add(centerHost);

            root.Controls.Add(leftBrand, 0, 0);
            root.Controls.Add(right, 1, 0);
            Controls.Add(root);
            AcceptButton = _btnLogin;
            Shown += LoginForm_Shown;
        }

        private void LoginForm_Shown(object sender, EventArgs e)
        {
            try
            {
                _cbLogin.Items.Clear();
                foreach (var x in LoginHistoryStore.Load())
                {
                    _cbLogin.Items.Add(x);
                }

                if (File.Exists(LastLoginPath))
                {
                    var last = File.ReadAllText(LastLoginPath).Trim();
                    if (last.Length > 0)
                    {
                        _cbLogin.Text = last;
                        if (!_cbLogin.Items.Contains(last))
                        {
                            _cbLogin.Items.Insert(0, last);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("LoginForm.Shown", ex);
            }

            _txtPassword.Text = string.Empty;
            _cbLogin.FlatStyle = FlatStyle.Standard;
            _cbLogin.Select();
        }

        private void LnkRemember_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                SanitizeLogin();
                var login = _cbLogin.Text.Trim();
                if (!InputValidators.IsValidLogin(login))
                {
                    _lblError.ForeColor = ThemeHelper.Danger;
                    _lblError.Text = "Сначала введите корректный логин (3..64 символа, без пробелов).";
                    return;
                }

                LoginHistoryStore.Remember(login);
                _lblError.Text = string.Empty;
                _tip.Show("Логин сохранён в списке", _lnkRemember, 0, _lnkRemember.Height + 4, 2600);
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("LoginForm.Remember", ex);
                _lblError.ForeColor = ThemeHelper.Danger;
                _lblError.Text = ex.Message;
            }
        }

        private void SanitizeLogin()
        {
            var t = _cbLogin.Text ?? string.Empty;
            t = Regex.Replace(t, @"\s+", string.Empty);
            if (t.Length > 64)
            {
                t = t.Substring(0, 64);
            }

            _cbLogin.Text = t;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            _lblError.ForeColor = ThemeHelper.Danger;
            _lblError.Text = string.Empty;
            try
            {
                SanitizeLogin();
                var login = _cbLogin.Text.Trim();
                var password = _txtPassword.Text;
                if (!InputValidators.IsValidLogin(login))
                {
                    _lblError.Text = "Логин: 3..64 символа, латиница/цифры/«_», без пробелов.";
                    return;
                }

                if (password.Length < 1)
                {
                    _lblError.Text = "Введите пароль.";
                    return;
                }

                if (AuthService.TryLogin(login, password, "127.0.0.1", out var error))
                {
                    SaveLastLogin(login);
                    LoginHistoryStore.Remember(login);
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                _lblError.Text = error;
            }
            catch (Exception ex)
            {
                ErrorLogService.LogUiException("LoginForm.BtnLogin_Click", ex);
                _lblError.Text = "Ошибка: " + ex.Message;
            }
        }

        private static void SaveLastLogin(string login)
        {
            try
            {
                File.WriteAllText(LastLoginPath, login ?? string.Empty);
            }
            catch
            {
                // ignore
            }
        }
    }
}
