using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClickMediaWorkTime.UI
{
    /// <summary>Панель со скруглением и мягкой обводкой — «изюминка» карточек.</summary>
    internal sealed class RoundedPanel : Panel
    {
        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }

        public int Radius { get; set; } = 16;

        public Color BorderColor { get; set; } = Color.FromArgb(220, 218, 210);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ThemeHelper.CreateRoundRectPath(rect, Radius))
            using (var fill = new SolidBrush(BackColor))
            {
                g.FillPath(fill, path);
                using (var pen = new Pen(BorderColor, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null)
            {
                using (var b = new SolidBrush(Parent.BackColor))
                {
                    e.Graphics.FillRectangle(b, ClientRectangle);
                }
            }
            else
            {
                base.OnPaintBackground(e);
            }
        }
    }
}
