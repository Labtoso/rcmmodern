using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RcmGui
{
    /// <summary>
    /// Einfaches Seitenmenue: der Hintergrund wird abgedunkelt, ein Panel gleitet von links herein.
    /// Klick auf die abgedunkelte Flaeche oder auf das Zahnrad schliesst.
    /// </summary>
    internal sealed class SettingsOverlay : Panel
    {
        private const int PanelW = 300;

        private readonly Scrim _scrim = new Scrim();
        private readonly DBPanel _panel = new DBPanel();
        private readonly Timer _anim = new Timer { Interval = 15 };

        private float _p;     // 0..1 Fortschritt
        private int _dir;

        private ToggleSwitch _auto;
        private readonly List<RoundButton> _langBtns = new List<RoundButton>();
        private readonly List<ColorSwatch> _themeSwatches = new List<ColorSwatch>();
        private readonly ToolTip _tips = new ToolTip();
        private GearButton _gear;
        private Label _titleLbl, _autoLbl, _autoHint, _langLbl, _themeLbl;

        public event Action Changed;
        public bool IsOpen { get; private set; }

        public SettingsOverlay()
        {
            Dock = DockStyle.Fill;
            Visible = false;

            _scrim.CloseRequested += Close;
            Controls.Add(_scrim);

            _panel.Width = PanelW;
            _panel.BackColor = Theme.Surface;
            _panel.Paint += PaintPanelEdge;
            Controls.Add(_panel);
            _panel.BringToFront();

            BuildPanel();

            _anim.Tick += Animate;
            Theme.Changed += () => { _panel.BackColor = Theme.Surface; ApplyTexts(); MarkSelection(); _panel.Invalidate(); };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _panel.Height = Height;
        }

        private void PaintPanelEdge(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
                e.Graphics.DrawLine(pen, _panel.Width - 1, 0, _panel.Width - 1, _panel.Height);
        }

        // ---------- Inhalt ----------

        private void BuildPanel()
        {
            _gear = new GearButton();
            _gear.Click += (s, e) => Close();
            _titleLbl = new Label { Font = Theme.Title, ForeColor = Theme.Text, AutoSize = true, Text = Strings.T("settings"), Location = new Point(36, 10) };
            // Zahnrad exakt auf die Mitte des Titeltexts zentrieren (gleiche Hoehe wie im Kopf)
            _gear.Location = new Point(0, _titleLbl.Top + (_titleLbl.PreferredHeight - _gear.Height) / 2);

            _autoLbl = new Label { Font = Theme.Label, ForeColor = Theme.Muted, AutoSize = true, Location = new Point(20, 74) };
            _auto = new ToggleSwitch { Location = new Point(20, 98) };
            _auto.Toggled += () => { Settings.AutoInject = _auto.On; Settings.Save(); Changed?.Invoke(); };
            _autoHint = new Label { Font = Theme.Small, ForeColor = Theme.Muted, Location = new Point(20, 132), Size = new Size(PanelW - 40, 48) };

            _langLbl = new Label { Font = Theme.Label, ForeColor = Theme.Muted, AutoSize = true, Location = new Point(20, 196) };
            BuildLangButtons(218);

            _themeLbl = new Label { Font = Theme.Label, ForeColor = Theme.Muted, AutoSize = true, Location = new Point(20, 278) };
            BuildThemeSwatches(300);

            _panel.Controls.AddRange(new Control[] { _gear, _titleLbl, _autoLbl, _auto, _autoHint, _langLbl, _themeLbl });

            ApplyTexts();
            MarkSelection();
            _auto.On = Settings.AutoInject;
        }

        private void BuildLangButtons(int y)
        {
            var w = (PanelW - 40 - 8) / 2;
            var items = new[] { ("de", "Deutsch"), ("en", "English") };
            for (var i = 0; i < items.Length; i++)
            {
                var code = items[i].Item1;
                var b = Theme.SecondaryButton(items[i].Item2);
                b.Size = new Size(w, 34);
                b.Location = new Point(20 + i * (w + 8), y);
                b.Tag = code;
                b.Click += (s, e) =>
                {
                    if (Settings.Lang == code) return;
                    Settings.Lang = code; Settings.Save();
                    ApplyTexts(); MarkSelection(); Changed?.Invoke();
                };
                _langBtns.Add(b);
                _panel.Controls.Add(b);
            }
        }

        private void BuildThemeSwatches(int y)
        {
            for (var i = 0; i < Theme.Themes.Length; i++)
            {
                var def = Theme.Themes[i];
                var sw = new ColorSwatch { Color = def.Accent, Location = new Point(20 + i * 50, y) };
                sw.Click += (s, e) =>
                {
                    if (Settings.Theme == def.Key) return;
                    Settings.Theme = def.Key; Settings.Save();
                    Theme.Apply(def.Key); MarkSelection(); Changed?.Invoke();
                };
                _themeSwatches.Add(sw);
                _panel.Controls.Add(sw);
            }
        }

        private void ApplyTexts()
        {
            _titleLbl.Text = Strings.T("settings");
            _autoLbl.Text = Strings.T("autoInject").ToUpperInvariant();
            _autoHint.Text = Strings.T("autoInjectHint");
            _langLbl.Text = Strings.T("language").ToUpperInvariant();
            _themeLbl.Text = Strings.T("theme").ToUpperInvariant();
            _titleLbl.ForeColor = Theme.Text;
            foreach (var l in new[] { _autoLbl, _autoHint, _langLbl, _themeLbl }) l.ForeColor = Theme.Muted;
        }

        private void MarkSelection()
        {
            foreach (var b in _langBtns) b.SetPrimary((string)b.Tag == Settings.Lang);
            for (var i = 0; i < _themeSwatches.Count; i++)
            {
                _themeSwatches[i].Selected = Theme.Themes[i].Key == Settings.Theme;
                _themeSwatches[i].Invalidate();
                _tips.SetToolTip(_themeSwatches[i], Settings.Lang == "en" ? Theme.Themes[i].En : Theme.Themes[i].De);
            }
        }

        // ---------- Oeffnen / Schliessen ----------

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _auto.On = Settings.AutoInject;
            ApplyTexts();
            MarkSelection();
            _scrim.TakeShot(Parent);   // echtes Fenster-Standbild (volle Groesse)
            _scrim.SetDim(0f);
            _p = 0f;
            _dir = 1;
            Visible = true;
            BringToFront();
            _panel.Height = Height;
            _panel.Left = -PanelW;
            _panel.BringToFront();
            _anim.Start();
        }

        public void Close()
        {
            if (!IsOpen || _dir < 0) return;
            _dir = -1;
            _anim.Start();
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

        private void Animate(object sender, EventArgs e)
        {
            _p += _dir * 0.09f;
            if (_dir > 0 && _p >= 1f) { _p = 1f; _anim.Stop(); }
            else if (_dir < 0 && _p <= 0f)
            {
                _p = 0f; _anim.Stop(); Visible = false; IsOpen = false;
                _scrim.Clear();
                return;
            }
            var ease = EaseOut(_p);
            _panel.Left = (int)(-PanelW + PanelW * ease);
            _scrim.SetDim(_p);
        }
    }

    /// <summary>
    /// Verwischter Hintergrund: nimmt EINMAL beim Oeffnen ein Standbild der App-Flaeche auf, verwischt es
    /// einmalig (kein Ruckeln pro Frame) und zeichnet es 1:1 (kein Skalieren -> kein Zoom).
    /// </summary>
    internal sealed class Scrim : Control
    {
        private Image _shot;
        private float _dim;
        private int _offX, _offY;   // Position der Scrim-Ebene im Fenster (Padding), fuer 1:1-Zeichnen

        public event Action CloseRequested;

        public Scrim()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Dock = DockStyle.Fill;
        }

        /// <summary>source = das Fenster. Nimmt dessen Client-Flaeche auf und verwischt sie einmalig.</summary>
        public void TakeShot(Control source)
        {
            _shot?.Dispose();
            try
            {
                var w = Math.Max(1, source.ClientSize.Width);
                var h = Math.Max(1, source.ClientSize.Height);
                _offX = source.Padding.Left;   // die Scrim-Ebene sitzt um das Fenster-Padding versetzt
                _offY = source.Padding.Top;

                var origin = source.PointToScreen(Point.Empty);
                using (var sharp = new Bitmap(w, h))
                {
                    using (var cg = Graphics.FromImage(sharp))
                        cg.CopyFromScreen(origin.X, origin.Y, 0, 0, new Size(w, h));

                    // Einmalig verwischen: klein rechnen, gross ziehen
                    var sw = Math.Max(1, w / 10);
                    var sh = Math.Max(1, h / 10);
                    var blur = new Bitmap(w, h);
                    using (var small = new Bitmap(sw, sh))
                    {
                        using (var g1 = Graphics.FromImage(small))
                        {
                            g1.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            g1.DrawImage(sharp, 0, 0, sw, sh);
                        }
                        using (var g2 = Graphics.FromImage(blur))
                        {
                            g2.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            g2.DrawImage(small, new Rectangle(0, 0, w, h));
                        }
                    }
                    _shot = blur;
                }
            }
            catch { _shot = null; }
            Invalidate();
        }

        public void Clear() { _shot?.Dispose(); _shot = null; Invalidate(); }
        public void SetDim(float v) { _dim = v; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e) { CloseRequested?.Invoke(); base.OnMouseDown(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (_shot != null) g.DrawImageUnscaled(_shot, -_offX, -_offY);     // 1:1, kein Skalieren
            else using (var b = new SolidBrush(Theme.Bg)) g.FillRectangle(b, ClientRectangle);

            var a = (int)(150 * Math.Max(0f, Math.Min(1f, _dim)));
            if (a > 0)
                using (var dim = new SolidBrush(Color.FromArgb(a, 0, 0, 0)))
                    g.FillRectangle(dim, ClientRectangle);
        }
    }

    /// <summary>Panel mit Doppelpufferung, damit das Einsliden ruckelfrei ist.</summary>
    internal sealed class DBPanel : Panel
    {
        public DBPanel() { DoubleBuffered = true; }
    }

    /// <summary>Ein-/Aus-Schalter im Theme-Stil.</summary>
    internal sealed class ToggleSwitch : Control
    {
        private bool _on;
        public event Action Toggled;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Size = new Size(46, 26);
            Cursor = Cursors.Default;
            Theme.Changed += Invalidate;
        }

        public bool On
        {
            get => _on;
            set { _on = value; Invalidate(); }
        }

        protected override void OnClick(EventArgs e)
        {
            _on = !_on;
            Invalidate();
            Toggled?.Invoke();
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var track = Theme.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Height / 2f))
            using (var fill = new SolidBrush(_on ? Theme.Accent : Theme.Border))
                g.FillPath(fill, track);
            var d = Height - 8;
            var kx = _on ? Width - d - 4 : 4;
            using (var knob = new SolidBrush(Color.White))
                g.FillEllipse(knob, kx, 4, d, d);
        }
    }
}
