using System.Drawing;
using System.Windows.Forms;

namespace LlamaServerLauncher
{
    // TabControl subclass that eliminates system-drawn tab borders in dark mode.
    // WM_ERASEBKGND fills the strip background; WM_PAINT post-draw overpaints each
    // tab after the system finishes rendering (otherwise FlatButton borders survive DrawItem).
    internal sealed class DarkTabControl : TabControl
    {
        private Color _stripColor = SystemColors.Control;

        public Color StripColor
        {
            get => _stripColor;
            set { _stripColor = value; Invalidate(); }
        }

        public Color SelectedTabColor   { get; set; } = Color.Empty;
        public Color UnselectedTabColor { get; set; } = Color.Empty;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0014 && _stripColor.ToArgb() != SystemColors.Control.ToArgb())
            {
                // WM_ERASEBKGND — fill strip background before system draws
                using var g = Graphics.FromHdc(m.WParam);
                using var br = new SolidBrush(_stripColor);
                g.FillRectangle(br, ClientRectangle);
                m.Result = (System.IntPtr)1;
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == 0x000F && SelectedTabColor != Color.Empty)
            {
                // WM_PAINT post-draw — overpaint each tab after system rendering to cover
                // the FlatButton borders that survive DrawItem
                using var g = CreateGraphics();
                for (int i = 0; i < TabCount; i++)
                {
                    bool sel = i == SelectedIndex;
                    var r = GetTabRect(i);
                    using var br = new SolidBrush(sel ? SelectedTabColor : UnselectedTabColor);
                    g.FillRectangle(br, Rectangle.Inflate(r, 3, 2));
                    var fg = sel ? Color.White : Color.FromArgb(140, 140, 140);
                    TextRenderer.DrawText(g, TabPages[i].Text, Font, r, fg,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }
    }
}
