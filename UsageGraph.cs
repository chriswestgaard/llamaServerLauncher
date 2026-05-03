using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LlamaServerLauncher
{
    internal sealed class UsageGraph : Control
    {
        private readonly Queue<float> _samples = new();
        private const int MaxSamples = 80;

        public Color  GraphColor { get; set; } = Color.LimeGreen;
        public string Title      { get; set; } = "";
        public string ValueText  { get; set; } = "";

        public void AddSample(float pct, string valueText)
        {
            while (_samples.Count >= MaxSamples) _samples.Dequeue();
            _samples.Enqueue(Math.Clamp(pct, 0f, 100f));
            ValueText = valueText;
            if (IsHandleCreated) Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width, h = Height;
            if (w <= 0 || h <= 0) return;

            // Background
            g.FillRectangle(new SolidBrush(Color.FromArgb(12, 12, 12)), 0, 0, w, h);

            // Horizontal grid lines at 25 / 50 / 75 %
            using (var gridPen = new Pen(Color.FromArgb(35, 35, 35)))
                for (int i = 1; i < 4; i++)
                    g.DrawLine(gridPen, 0, h * i / 4, w, h * i / 4);

            // Graph fill + line
            var samples = _samples.ToArray();
            if (samples.Length >= 2)
            {
                float step   = w / (float)(MaxSamples - 1);
                int   offset = MaxSamples - samples.Length;
                int   inner  = h - 2;

                var pts = new PointF[samples.Length + 2];
                pts[0] = new PointF(offset * step, h);
                for (int i = 0; i < samples.Length; i++)
                    pts[i + 1] = new PointF((offset + i) * step, h - samples[i] / 100f * inner);
                pts[^1] = new PointF(w, h);

                using (var fill = new SolidBrush(Color.FromArgb(65, GraphColor)))
                    g.FillPolygon(fill, pts);

                using (var pen = new Pen(GraphColor, 1.5f))
                    g.DrawLines(pen, pts.Skip(1).Take(samples.Length).ToArray());
            }

            // Border
            using (var border = new Pen(Color.FromArgb(55, 55, 55)))
                g.DrawRectangle(border, 0, 0, w - 1, h - 1);

            // Labels
            using var font        = new Font("Segoe UI", 7.5f);
            using var titleBrush  = new SolidBrush(Color.FromArgb(160, 160, 160));
            using var valueBrush  = new SolidBrush(GraphColor);

            g.DrawString(Title, font, titleBrush, 4f, 3f);

            if (!string.IsNullOrEmpty(ValueText))
            {
                var sz = g.MeasureString(ValueText, font);
                g.DrawString(ValueText, font, valueBrush, w - sz.Width - 4f, 3f);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
    }
}
