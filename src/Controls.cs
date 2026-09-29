using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RcmGui
{
    /// <summary>Karte mit runden Ecken und 1px-Rand. Kinder bekommen die Kartenfarbe.</summary>
    internal sealed class Card : Panel
    {
        public Card()
        {
            BackColor = Theme.Bg; // Ecken ausserhalb der Rundung
            Padding = new Padding(14);
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (!(e.Control is Button)) e.Control.BackColor = Theme.Surface;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.Scale(g, Theme.CardRadius)))
            using (var fill = new SolidBrush(Theme.Surface))
            using (var pen = new Pen(Theme.Border))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>Knopf mit runden Ecken und Hover-Zustand. Farben folgen dem Theme.</summary>
    internal sealed class RoundButton : Button
    {
        private ButtonKind _kind;
        private bool _hover;

        public void SetPrimary(bool primary)
        {
            var k = primary ? ButtonKind.Primary : ButtonKind.Secondary;
            if (_kind == k) return;
            _kind = k;
            Invalidate();
        }

        public RoundButton(ButtonKind kind = ButtonKind.Secondary)
        {
            _kind = kind;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = Theme.Body;
            Height = 34;
            Cursor = Cursors.Default;
            Theme.Changed += Invalidate;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= Invalidate;
            base.Dispose(disposing);
        }

        protected override bool ShowFocusCues => false;

        protected override void OnMouseEnter(System.EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(System.EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnEnabledChanged(System.EventArgs e)
        {
            Cursor = Cursors.Default;
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent is Card ? Theme.Surface : Parent?.BackColor ?? Theme.Bg); // Card malt Surface selbst
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var primary = _kind == ButtonKind.Primary;
            var baseFill = primary ? Theme.Accent : Theme.Surface;
            var fill = !Enabled ? Theme.Surface : _hover ? (primary ? Theme.AccentHover : Theme.SurfaceHover) : baseFill;
            var border = Enabled ? (primary ? Theme.Accent : Theme.Border) : Theme.Border;
            var text = Enabled ? (primary ? Color.White : Theme.Text) : Theme.Muted;

            using (var path = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.Scale(g, Theme.ButtonRadius)))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>Runder Icon-Knopf, zeichnet ein Zahnrad. Ohne Fremd-Schriftart.</summary>
    internal sealed class GearButton : Control
    {
        private bool _hover;

        public GearButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Size = new Size(28, 28);
            Cursor = Cursors.Default;
            Theme.Changed += Invalidate;
        }

        protected override void OnMouseEnter(System.EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(System.EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover)
                using (var hb = new SolidBrush(Theme.SurfaceHover))
                using (var hp = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.Scale(g, Theme.ButtonRadius)))
                    g.FillPath(hb, hp);

            // Klassisches Zahnrad: Segoe-MDL2-Symbol (Standard-Windows-Einstellungssymbol)
            var color = _hover ? Theme.Text : Theme.Muted;
            var glyph = ""; // MDL2 "Settings"
            using (var f = new Font("Segoe MDL2 Assets", Height * 0.7f, GraphicsUnit.Pixel))
            {
                var sz = TextRenderer.MeasureText(g, glyph, f);
                if (sz.Width > 2)
                {
                    TextRenderer.DrawText(g, glyph, f, ClientRectangle, color,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    return;
                }
            }
            // Fallback: klassisches Zahnrad selbst zeichnen (falls die Schrift fehlt)
            var cx = Width / 2f; var cy = Height / 2f;
            var rOut = Math.Min(Width, Height) * 0.32f;
            var rMid = rOut * 0.72f;
            var rIn = rOut * 0.40f;
            const int teeth = 8;
            using (var path = new GraphicsPath())
            {
                var pts = new List<PointF>();
                var steps = teeth * 4;
                for (var i = 0; i < steps; i++)
                {
                    var ang = Math.PI * 2 * i / steps - Math.PI / 2;
                    var rr = (i % 4 == 0 || i % 4 == 1) ? rOut : rMid; // flache Zahnkoepfe
                    pts.Add(new PointF(cx + (float)(Math.Cos(ang) * rr), cy + (float)(Math.Sin(ang) * rr)));
                }
                path.AddPolygon(pts.ToArray());
                using (var brush = new SolidBrush(color))
                    g.FillPath(brush, path);
            }
            using (var hole = new SolidBrush(_hover ? Theme.SurfaceHover : (Parent?.BackColor ?? Theme.Bg)))
                g.FillEllipse(hole, cx - rIn, cy - rIn, rIn * 2, rIn * 2);
        }
    }

    /// <summary>Runder Farb-Knopf (Theme-Akzent). Ausgewaehlt bekommt einen Ring.</summary>
    internal sealed class ColorSwatch : Control
    {
        public Color Color;
        public bool Selected;
        private bool _hover;

        public ColorSwatch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Size = new Size(40, 40);
            Cursor = Cursors.Default;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Alles konzentrisch um die Mitte
            var cx = Width / 2f;
            var cy = Height / 2f;
            var outer = Math.Min(Width, Height) / 2f - 2f;   // Ring-Radius
            var fillR = Selected ? outer - 4.5f : outer;     // Fuellkreis, bei Auswahl kleiner (Platz fuer Ring)

            if (Selected)
                using (var ring = new Pen(Color, 2.5f))
                    g.DrawEllipse(ring, cx - outer, cy - outer, outer * 2, outer * 2);

            using (var fill = new SolidBrush(Color))
                g.FillEllipse(fill, cx - fillR, cy - fillR, fillR * 2, fillR * 2);

            if (Selected)
                using (var check = new Pen(Color.White, 2.2f))
                    g.DrawLines(check, new[] {
                        new PointF(cx - 5, cy),
                        new PointF(cx - 1.5f, cy + 3.5f),
                        new PointF(cx + 5, cy - 4)
                    });
            else if (_hover)
                using (var hb = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    g.FillEllipse(hb, cx - fillR, cy - fillR, fillR * 2, fillR * 2);
        }
    }

    /// <summary>Kleine Ueberschrift in Grossbuchstaben.</summary>
    internal sealed class SectionLabel : Label
    {
        public SectionLabel(string text)
        {
            Text = text.ToUpperInvariant();
            Font = Theme.Label;
            ForeColor = Theme.Muted;
            AutoSize = true;
            Dock = DockStyle.Top;
            Padding = new Padding(0, 0, 0, 8);
        }
    }

    /// <summary>Status-Anzeige rechts oben: Punkt + Text in einer Pille.</summary>
    internal sealed class StatusPill : Control
    {
        private Color _dot = Theme.Muted;

        public StatusPill()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Font = Theme.Small;
            ForeColor = Theme.Text;
            BackColor = Theme.Bg;
            Height = 28;
        }

        public void Set(string text, Color dot)
        {
            Text = text;
            _dot = dot;
            using (var g = CreateGraphics())
                Width = (int)g.MeasureString(text, Font).Width + Height + 8;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Height / 2f))
            using (var fill = new SolidBrush(Theme.Surface))
            using (var pen = new Pen(Theme.Border))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            var d = Height / 3;
            using (var dot = new SolidBrush(_dot))
                g.FillEllipse(dot, Height / 2 - d / 2 + 2, (Height - d) / 2, d, d);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(Height - 2, 0, Width - Height, Height), ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}
