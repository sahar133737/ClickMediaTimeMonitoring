using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClickMediaWorkTime.UI
{
    /// <summary>
    /// Визуальная тема ИС «Клик Медиа»: тёмный сайдбар, тёплый акцент (терракота), морская волна для контуров, золотая «перфорация» как отсылка к production.</summary>
    internal static class ThemeHelper
    {
        public const int FormFieldLabelHeight = 18;

        public static readonly Font UiFont = new Font("Segoe UI", 10f);
        public static readonly Font UiFontSemi = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
        public static readonly Font HeroFont = new Font("Segoe UI Semibold", 18f, FontStyle.Bold);

        public static readonly Color ShellBg = Color.FromArgb(18, 20, 28);
        public static readonly Color ShellPanel = Color.FromArgb(28, 31, 42);
        public static readonly Color ShellBorder = Color.FromArgb(55, 62, 82);
        /// <summary>Основной CTA — тёплая терракота (не «ещё один фиолетовый SaaS»).</summary>
        public static readonly Color Brand = Color.FromArgb(215, 113, 90);
        /// <summary>Вторичный акцент и подсветка контуров.</summary>
        public static readonly Color Mint = Color.FromArgb(78, 205, 196);
        /// <summary>«Киноплёнка» — тонкие акценты на тёмном фоне.</summary>
        public static readonly Color FilmGold = Color.FromArgb(255, 198, 90);
        /// <summary>Нейтральные кнопки (закрыть, обновить).</summary>
        public static readonly Color Accent = Color.FromArgb(95, 105, 138);
        public static readonly Color Accent2 = Mint;
        public static readonly Color Danger = Color.FromArgb(232, 86, 104);
        public static readonly Color Warn = Color.FromArgb(255, 189, 105);

        public static readonly Color Primary = Brand;
        public static readonly Color Secondary = Color.FromArgb(60, 64, 86);

        public static readonly Color Surface = Color.FromArgb(244, 245, 248);
        public static readonly Color Card = Color.White;
        public static readonly Color Text = Color.FromArgb(24, 28, 42);
        public static readonly Color MutedText = Color.FromArgb(102, 112, 138);
        public static readonly Color GridHeader = Color.FromArgb(232, 236, 242);
        public static readonly Color GridSelect = Color.FromArgb(255, 236, 228);

        public static Label FormFieldLabel(string text, int left, int top, int width, Color? foreColor = null)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = FormFieldLabelHeight,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = foreColor ?? MutedText,
                BackColor = Color.Transparent,
                Font = UiFont
            };
        }

        public static void ApplyForm(Form form, string title)
        {
            form.Text = title;
            form.BackColor = Surface;
            form.Font = UiFont;
            form.StartPosition = FormStartPosition.CenterParent;
            form.Load += (s, e) => ApplyMinimalistTheme(form);
        }

        public static void StylePrimary(Button button)
        {
            StyleModernButton(button, Brand);
        }

        public static void StyleSecondary(Button button)
        {
            StyleModernButton(button, Color.FromArgb(60, 64, 86));
        }

        public static void StyleGhostOnDark(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ShellBorder;
            button.BackColor = Color.FromArgb(32, 36, 56);
            button.ForeColor = Color.FromArgb(230, 233, 245);
            button.Font = UiFontSemi;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 78);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(28, 32, 52);
            button.Height = Math.Max(button.Height, 42);
        }

        public static void StyleAccentOutline(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Mint;
            button.BackColor = Color.FromArgb(26, 30, 48);
            button.ForeColor = Mint;
            button.Font = UiFontSemi;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 40, 62);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(22, 26, 42);
            button.Height = Math.Max(button.Height, 42);
        }

        /// <summary>Плоская кнопка панели отчётов: светлый фон, акцентная обводка.</summary>
        public static void StyleReportOutline(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(210, 200, 195);
            button.BackColor = Color.FromArgb(252, 252, 253);
            button.ForeColor = Text;
            button.Font = UiFontSemi;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 244, 240);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 228, 218);
            button.Height = Math.Max(button.Height, 40);
            button.MinimumSize = new Size(160, 40);
        }

        public static void StyleDanger(Button button)
        {
            StyleModernButton(button, Danger);
        }

        /// <summary>Совместимость со старыми вызовами ThemeHelper.StyleButton(..., Primary).</summary>
        public static void StyleButton(Button button, Color backColor)
        {
            StyleModernButton(button, backColor);
        }

        private static void StyleModernButton(Button button, Color backColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.Font = UiFontSemi;
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.08f);
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.06f);
            button.Height = Math.Max(button.Height, 36);
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleVertical;
            grid.GridColor = Color.FromArgb(230, 232, 240);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = UiFontSemi;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = GridSelect;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.BackColor = Card;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 255);
            grid.RowTemplate.Height = 28;
        }

        public static Panel CreateSidebar(int width)
        {
            var p = new Panel
            {
                Dock = DockStyle.Left,
                Width = width,
                BackColor = ShellPanel
            };
            p.Paint += Sidebar_Paint;
            return p;
        }

        private static void Sidebar_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel p))
            {
                return;
            }

            using (var pen = new Pen(Color.FromArgb(60, 70, 110), 1))
            {
                e.Graphics.DrawLine(pen, p.Width - 1, 0, p.Width - 1, p.Height);
            }

            if (p.Height <= 1)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new LinearGradientBrush(
                       new Rectangle(0, 0, 10, p.Height),
                       Color.FromArgb(140, Brand.R, Brand.G, Brand.B),
                       Color.FromArgb(0, Brand.R, Brand.G, Brand.B),
                       LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(b, 0, 0, 7, p.Height);
            }

            // «Перфорация» вдоль левого края — изюминка бренда production
            const int holeH = 5;
            const int gap = 9;
            using (var holeBrush = new SolidBrush(Color.FromArgb(18, 20, 28)))
            using (var gold = new Pen(Color.FromArgb(180, FilmGold.R, FilmGold.G, FilmGold.B), 1f))
            {
                for (var y = 12; y < p.Height - holeH; y += gap)
                {
                    var r = new Rectangle(2, y, 5, holeH);
                    e.Graphics.FillEllipse(holeBrush, r);
                    e.Graphics.DrawEllipse(gold, r);
                }
            }
        }

        public static GraphicsPath CreateRoundRectPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            var r = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
            var d = r * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static GroupBox CreateCardGroup(string title, int dockTopHeight)
        {
            return new GroupBox
            {
                Dock = DockStyle.Top,
                Height = dockTopHeight,
                Text = "  " + title + "  ",
                ForeColor = Text,
                BackColor = Card,
                Font = UiFontSemi,
                Padding = new Padding(12)
            };
        }

        public static void ApplyMinimalistTheme(Control root)
        {
            if (root is Form form)
            {
                form.BackColor = Surface;
                form.ForeColor = Text;
            }

            foreach (Control child in root.Controls)
            {
                if (child is DataGridView grid)
                {
                    StyleGrid(grid);
                }
                else if (child is GroupBox group)
                {
                    group.ForeColor = Text;
                    group.BackColor = Card;
                    group.Padding = new Padding(12);
                }
                else if (child is Panel panel)
                {
                    if (panel.Dock != DockStyle.Left && panel.Dock != DockStyle.Top)
                    {
                        if (panel.BackColor == default(Color) || panel.BackColor == SystemColors.Control)
                        {
                            panel.BackColor = Surface;
                        }
                    }
                }
                else if (child is Label label)
                {
                    label.ForeColor = IsDarkBackground(label.Parent?.BackColor ?? Surface) ? Color.FromArgb(235, 238, 250) : Text;
                    label.BackColor = Color.Transparent;
                }
                else if (child is TextBox textBox)
                {
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = Color.White;
                    textBox.ForeColor = Text;
                }
                else if (child is ComboBox comboBox)
                {
                    comboBox.BackColor = Color.White;
                    comboBox.ForeColor = Text;
                    comboBox.FlatStyle = FlatStyle.Flat;
                }
                else if (child is DateTimePicker datePicker)
                {
                    datePicker.CalendarForeColor = Text;
                    datePicker.CalendarMonthBackground = Color.White;
                }
                else if (child is NumericUpDown numeric)
                {
                    numeric.BackColor = Color.White;
                    numeric.ForeColor = Text;
                }
                else if (child is CheckBox checkBox)
                {
                    checkBox.UseVisualStyleBackColor = true;
                    checkBox.FlatStyle = FlatStyle.Standard;
                    checkBox.ForeColor = IsDarkBackground(checkBox.Parent?.BackColor ?? Surface) ? Color.FromArgb(235, 238, 250) : Text;
                }
                else if (child is Button button)
                {
                    if (button.BackColor == SystemColors.Control || button.BackColor == default(Color))
                    {
                        StylePrimary(button);
                    }
                }

                if (child.HasChildren)
                {
                    ApplyMinimalistTheme(child);
                }
            }
        }

        private static bool IsDarkBackground(Color color)
        {
            var brightness = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
            return brightness < 140;
        }
    }
}
