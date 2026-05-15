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

        // ── Fixed-scale mode (CPU / RAM / GPU / VRAM) ──────────────────────
        public void AddSample(float pct, string valueText)
        {
            _ceiling     = 100f;
            _absoluteMax = 100f;
            Enqueue(pct);
            ValueText = valueText;
            if (IsHandleCreated) Invalidate();
        }

        // ── Dynamic-scale mode (Context) ────────────────────────────────────
        // ceiling grows in powers-of-two (1024, 2048, 4096 …) as value rises,
        // then is capped at maximum. Resets automatically on model change.
        private float _ceiling     = 0f;
        private float _absoluteMax = 100f;

        public void AddSample(float value, float maximum, string valueText)
        {
            if (maximum < 1f) maximum = 1f;

            // Reset ceiling whenever the hard maximum changes (new model loaded).
            if (Math.Abs(maximum - _absoluteMax) > 0.5f)
            {
                _absoluteMax = maximum;
                _ceiling     = 0f;
            }

            if (_ceiling <= 0f) _ceiling = NiceCeiling(1024f, maximum);
            if (value > _ceiling * 0.85f) _ceiling = NiceCeiling(value * 1.25f, maximum);

            Enqueue(value);
            ValueText = valueText;
            if (IsHandleCreated) Invalidate();
        }

        private void Enqueue(float v)
        {
            while (_samples.Count >= MaxSamples) _samples.Dequeue();
            _samples.Enqueue(v);
        }

        // Next power-of-two ≥ 1024 that fits above target, capped at maximum.
        private static float NiceCeiling(float target, float maximum)
        {
            float c = 1024f;
            while (c < target && c < maximum) c *= 2f;
            return Math.Min(c, maximum);
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
                float scale  = _ceiling > 0f ? 100f / _ceiling : 1f;
                float step   = w / (float)(MaxSamples - 1);
                int   offset = MaxSamples - samples.Length;
                int   inner  = h - 2;

                var pts = new PointF[samples.Length + 2];
                pts[0] = new PointF(offset * step, h);
                for (int i = 0; i < samples.Length; i++)
                {
                    float pct = Math.Clamp(samples[i] * scale, 0f, 100f);
                    pts[i + 1] = new PointF((offset + i) * step, h - pct / 100f * inner);
                }
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
