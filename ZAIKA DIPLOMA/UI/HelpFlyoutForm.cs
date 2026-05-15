using System.Drawing;
using System.Windows.Forms;

namespace ClickMediaWorkTime.UI
{
    /// <summary>Компактное окно справки (без перегрузки экрана).</summary>
    internal sealed class HelpFlyoutForm : Form
    {
        public HelpFlyoutForm(string title, string body)
        {
            Text = "Справка";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Width = 560;
            Height = 420;
            BackColor = ThemeHelper.Surface;

            var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = ThemeHelper.ShellPanel };
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 0),
                ForeColor = Color.White,
                Font = ThemeHelper.TitleFont,
                Text = title
            };
            header.Controls.Add(lbl);

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = ThemeHelper.Text,
                Font = ThemeHelper.UiFont,
                Margin = new Padding(14),
                Text = body
            };

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = ThemeHelper.Surface };
            var ok = new Button { Text = "Понятно", Width = 140, Height = 36, Anchor = AnchorStyles.Right };
            ok.Left = bottom.Width - ok.Width - 16;
            ok.Top = 8;
            bottom.Resize += (s, e) => ok.Left = bottom.ClientSize.Width - ok.Width - 16;
            ThemeHelper.StylePrimary(ok);
            ok.Click += (s, e) => Close();
            bottom.Controls.Add(ok);

            Controls.Add(rtb);
            Controls.Add(bottom);
            Controls.Add(header);
            AcceptButton = ok;
        }
    }
}
