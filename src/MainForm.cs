using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RcmGui
{
    internal sealed class MainForm : Form
    {
        private readonly StatusPill _status = new StatusPill();
        private readonly ListBox _list = new ListBox();
        private readonly Label _selName = new Label();
        private readonly Button _inject = Theme.PrimaryButton("Payload senden");
        private readonly Button _add = Theme.SecondaryButton("Hinzufügen");
        private readonly Button _remove = Theme.SecondaryButton("Entfernen");
        private readonly RichTextBox _log = new RichTextBox();
        private readonly Timer _poll = new Timer { Interval = 1500 };
        private readonly ToolTip _tips = new ToolTip { InitialDelay = 500, AutoPopDelay = 8000, ReshowDelay = 200 };
        private readonly GearButton _gear = new GearButton();
        private readonly SettingsOverlay _settings = new SettingsOverlay();

        // fuer Sprache/Theme-Wechsel gemerkte Steuerelemente
        private Label _title;
        private SectionLabel _lblPayloads, _lblSelected, _lblLog;
        private TableLayoutPanel _root, _content;
        private Card _leftCard, _rightCard, _logCard;

        private RcmState _state = RcmState.NotFound;
        private bool _polling, _injecting, _driverBusy, _driverTried;

        public MainForm()
        {
            Text = "RcmGui";
            Icon = Theme.AppIcon();
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(780, 560);
            MinimumSize = new Size(640, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Padding = new Padding(16);
            AllowDrop = true;

            BuildLayout();

            foreach (var f in Favorites.Load()) _list.Items.Add(f);
            if (_list.Items.Count > 0) _list.SelectedIndex = 0;

            _list.SelectedIndexChanged += (s, e) => UpdateUi();
            _add.Click += (s, e) => AddPayload();
            _remove.Click += (s, e) => RemovePayload();
            _inject.Click += async (s, e) => await InjectAsync();
            _poll.Tick += (s, e) => Poll();
            DragEnter += (s, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            DragDrop += (s, e) => { foreach (var f in (string[])e.Data.GetData(DataFormats.FileDrop)) AddToList(f, true); };

            // Hinweise nur beim Draufbleiben, keine festen Texte in der Oberflaeche
            _gear.Click += (s, e) => { if (_settings.IsOpen) _settings.Close(); else _settings.Open(); };
            _settings.Changed += OnSettingsChanged;
            Controls.Add(_settings);
            _settings.BringToFront();

            Theme.Changed += () => { ApplyThemeColors(); Localize(); };

            Localize();
            SetState(RcmState.NotFound);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyTitleBar(Handle);
        }

        private void OnSettingsChanged()
        {
            ApplyThemeColors();
            Localize();
            // Auto-Inject direkt anwenden, falls schon bereit
            MaybeAutoInject();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _poll.Start();
            Poll();
            await InitAsync();
        }

        // ---------- Aufbau ----------

        private void BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Bg };
            _root = root;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            Controls.Add(root);

            // Kopfzeile
            var header = new Panel { Dock = DockStyle.Fill, Height = 44, Margin = new Padding(0, 0, 0, 12) };
            _title = new Label { Text = "RcmGui", Font = Theme.Title, AutoSize = true, Location = new Point(36, 10), ForeColor = Theme.Text };
            // Zahnrad exakt auf die Mitte des Titeltexts zentrieren
            _gear.Location = new Point(0, _title.Top + (_title.PreferredHeight - _gear.Height) / 2);
            _status.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            header.Controls.Add(_gear);
            header.Controls.Add(_title);
            header.Controls.Add(_status);
            header.Resize += (s, e) => _status.Location = new Point(header.Width - _status.Width, 6);
            _status.SizeChanged += (s, e) => _status.Location = new Point(header.Width - _status.Width, 6);
            root.Controls.Add(header, 0, 0);

            // Inhalt: Payload-Liste links, Auswahl rechts
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            _content = content;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.Controls.Add(content, 0, 1);

            var left = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
            _leftCard = left;
            _list.Dock = DockStyle.Fill;
            _list.BorderStyle = BorderStyle.None;
            _list.BackColor = Theme.Surface;
            _list.ForeColor = Theme.Text;
            _list.DrawMode = DrawMode.OwnerDrawFixed;
            _list.IntegralHeight = false;
            _list.DrawItem += DrawPayloadItem;
            using (var g = CreateGraphics()) _list.ItemHeight = (int)(36 * g.DpiY / 96f);

            var leftButtons = new TableLayoutPanel { Dock = DockStyle.Bottom, ColumnCount = 2, Height = 46, Padding = new Padding(0, 12, 0, 0) };
            leftButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            leftButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _add.Dock = DockStyle.Fill; _add.Margin = new Padding(0, 0, 4, 0);
            _remove.Dock = DockStyle.Fill; _remove.Margin = new Padding(4, 0, 0, 0);
            leftButtons.Controls.Add(_add, 0, 0);
            leftButtons.Controls.Add(_remove, 1, 0);

            _lblPayloads = new SectionLabel("Payloads");
            left.Controls.Add(_list);
            left.Controls.Add(leftButtons);
            left.Controls.Add(_lblPayloads);
            content.Controls.Add(left, 0, 0);

            var right = new Card { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0) };
            _rightCard = right;
            _selName.Font = Theme.Title; _selName.ForeColor = Theme.Text; _selName.Dock = DockStyle.Top; _selName.AutoEllipsis = true; _selName.Height = 30;
            _inject.Dock = DockStyle.Bottom; _inject.Height = 40;

            right.Controls.Add(_inject);
            right.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 12 });
            _lblSelected = new SectionLabel("Ausgewählt");
            right.Controls.Add(_selName);
            right.Controls.Add(_lblSelected);
            right.Controls.SetChildIndex(_inject, right.Controls.Count - 1); // Senden ganz unten
            content.Controls.Add(right, 1, 0);

            // Protokoll
            var logCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0), Padding = new Padding(14, 10, 14, 10) };
            _logCard = logCard;
            _log.Dock = DockStyle.Fill;
            _log.ReadOnly = true;
            _log.BorderStyle = BorderStyle.None;
            _log.BackColor = Theme.Surface;
            _log.ForeColor = Theme.Muted;
            _log.Font = Theme.Mono;
            _log.DetectUrls = false;
            _lblLog = new SectionLabel("Protokoll");
            logCard.Controls.Add(_log);
            logCard.Controls.Add(_lblLog);
            root.Controls.Add(logCard, 0, 2);
        }

        // ---------- Sprache & Theme ----------

        private void Localize()
        {
            _lblPayloads.Text = Strings.T("payloads").ToUpperInvariant();
            _lblSelected.Text = Strings.T("selected").ToUpperInvariant();
            _lblLog.Text = Strings.T("log").ToUpperInvariant();
            _add.Text = Strings.T("add");
            _remove.Text = Strings.T("remove");
            _tips.SetToolTip(_inject, Strings.T("tipInject"));
            _tips.SetToolTip(_add, Strings.T("tipAdd"));
            _tips.SetToolTip(_remove, Strings.T("tipRemove"));
            _tips.SetToolTip(_status, Strings.T("tipStatus"));
            _tips.SetToolTip(_gear, Strings.T("tipSettings"));
            UpdateUi();
            SetState(_state); // Status-Pille neu beschriften
        }

        private void ApplyThemeColors()
        {
            BackColor = Theme.Bg;
            _root.BackColor = Theme.Bg;
            _content.BackColor = Theme.Bg;
            _title.ForeColor = Theme.Text;
            _selName.ForeColor = Theme.Text;
            _list.BackColor = Theme.Surface;
            _list.ForeColor = Theme.Text;
            _log.BackColor = Theme.Surface;
            _log.ForeColor = Theme.Muted;
            foreach (var c in new Control[] { _root, _content, _leftCard, _rightCard, _logCard })
                c.Invalidate(true);
            if (IsHandleCreated) Theme.ApplyTitleBar(Handle);
            _list.Invalidate();
        }

        private void MaybeAutoInject()
        {
            if (Settings.AutoInject && !_injecting && _state == RcmState.Ready
                && Backend.IsReady && _list.SelectedItem is string)
                _ = InjectAsync();
        }

        private void DrawPayloadItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var path = (string)_list.Items[e.Index];
            var selected = (e.State & DrawItemState.Selected) != 0;
            var g = e.Graphics;

            using (var bg = new SolidBrush(Theme.Surface))
                g.FillRectangle(bg, e.Bounds);
            if (selected)
            {
                // Runde Auswahl mit blauem Pillen-Balken links
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var r = new RectangleF(e.Bounds.X + 1, e.Bounds.Y + 2, e.Bounds.Width - 3, e.Bounds.Height - 4);
                using (var shape = Theme.RoundRect(r, Theme.Scale(g, Theme.ButtonRadius)))
                using (var sel = new SolidBrush(Theme.SurfaceHover))
                    g.FillPath(sel, shape);
                using (var bar = Theme.RoundRect(new RectangleF(r.X + 4, r.Y + 8, 3, r.Height - 16), 1.5f))
                using (var accent = new SolidBrush(Theme.Accent))
                    g.FillPath(accent, bar);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
            }

            var hekate = Hekate.IsHekate(path);
            var name = hekate ? "Hekate " + Hekate.Version(path) : Path.GetFileName(path);
            TextRenderer.DrawText(g, name, Theme.Body, new Rectangle(e.Bounds.X + 14, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height), Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // ---------- Ablauf ----------

        private async Task InitAsync()
        {
            try
            {
                await Backend.EnsureAsync(Log);
            }
            catch (Exception ex)
            {
                Log("TegraRcmSmash konnte nicht geladen werden: " + ex.Message, Theme.Error);
            }

            string hekate;
            try
            {
                hekate = await Hekate.GetLatestAsync(Log);
            }
            catch (Exception ex)
            {
                hekate = Hekate.FindLocal();
                Log(hekate == null
                    ? "Hekate konnte nicht geladen werden: " + ex.Message
                    : "Offline, nutze vorhandenes Hekate " + Hekate.Version(hekate) + ".", hekate == null ? Theme.Error : Theme.Warn);
            }

            if (hekate != null)
            {
                for (var i = _list.Items.Count - 1; i >= 0; i--)
                    if (Hekate.IsHekate((string)_list.Items[i])) _list.Items.RemoveAt(i);
                _list.Items.Insert(0, hekate);
                _list.SelectedIndex = 0; // Hekate vorauswaehlen
            }
            UpdateUi();
        }

        private void Poll()
        {
            if (_polling) return;
            _polling = true;
            Task.Run(RcmDevice.Detect).ContinueWith(t =>
            {
                _polling = false;
                if (!t.IsFaulted && t.Result != _state) SetState(t.Result);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void SetState(RcmState state)
        {
            var changed = state != _state;
            _state = state;
            switch (state)
            {
                case RcmState.Ready:
                    _status.Set(Strings.T("stReady"), Theme.Ok);
                    if (changed) Log(Strings.T("stReady"), Theme.Ok);
                    break;
                case RcmState.NoDriver:
                    _status.Set(Strings.T("stNoDriver"), Theme.Warn);
                    if (changed) Log(Strings.T("stNoDriver"), Theme.Warn);
                    break;
                default:
                    _status.Set(Strings.T("stNone"), Theme.Muted);
                    break;
            }
            UpdateUi();
            if (changed) MaybeAutoInject();
            if (changed && state == RcmState.NoDriver) MaybeInstallDriver();
        }

        // Switch erkannt, aber libusbK-Treiber fehlt -> nachfragen und bei Ja installieren (einmal pro Sitzung)
        private async void MaybeInstallDriver()
        {
            if (_driverBusy || _driverTried || !Driver.ToolAvailable) return;
            _driverTried = true;   // nur einmal fragen (wie bei TegraRcmGUI)

            var answer = MessageBox.Show(this, Strings.T("drvAsk"), Strings.T("drvAskTitle"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            _driverBusy = true;
            Log(Strings.T("drvInstalling"), Theme.Warn);
            var r = await Driver.InstallAsync();
            switch (r)
            {
                case Driver.Result.Ok:
                    Log(Strings.T("drvOk"), Theme.Ok);
                    _driverTried = false; // nach Erfolg wieder freigeben
                    break;
                case Driver.Result.Cancelled:
                    Log(Strings.T("drvCancel"), Theme.Muted);
                    break;
                default:
                    Log(Strings.T("drvFail"), Theme.Error);
                    break;
            }
            _driverBusy = false;
            Poll();
        }

        private void UpdateUi()
        {
            var path = _list.SelectedItem as string;
            _selName.Text = path == null ? Strings.T("noPayload")
                : Hekate.IsHekate(path) ? "Hekate " + Hekate.Version(path) : Path.GetFileName(path);
            _tips.SetToolTip(_selName, path); // voller Pfad beim Draufbleiben

            _remove.Enabled = path != null && !Hekate.IsHekate(path);
            _inject.Enabled = !_injecting && path != null && _state == RcmState.Ready && Backend.IsReady;
            _inject.Text = _injecting ? Strings.T("injecting") : Strings.T("inject");
        }

        private async Task InjectAsync()
        {
            var path = _list.SelectedItem as string;
            if (path == null || !File.Exists(path))
            {
                Log("Payload nicht gefunden.", Theme.Error);
                return;
            }

            _injecting = true;
            UpdateUi();
            Log("Sende " + Path.GetFileName(path) + " ...");
            try
            {
                var (ok, output) = await Backend.InjectAsync(path);
                foreach (var line in output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
                    Log("  " + line);
                Log(ok ? "Payload erfolgreich gesendet." : "Senden fehlgeschlagen.", ok ? Theme.Ok : Theme.Error);
            }
            catch (Exception ex)
            {
                Log("Fehler: " + ex.Message, Theme.Error);
            }
            finally
            {
                _injecting = false;
                UpdateUi();
            }
        }

        private void AddPayload()
        {
            using (var dlg = new OpenFileDialog { Title = "Payload wählen", Filter = "Payload (*.bin)|*.bin|Alle Dateien (*.*)|*.*", Multiselect = true })
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    foreach (var f in dlg.FileNames) AddToList(f, true);
        }

        private void AddToList(string file, bool select)
        {
            if (!File.Exists(file)) return;
            var existing = _list.Items.Cast<string>().FirstOrDefault(p => string.Equals(p, file, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                _list.Items.Add(file);
                Favorites.Save(_list.Items.Cast<string>());
                Log("Gemerkt: " + Path.GetFileName(file));
            }
            if (select) _list.SelectedItem = existing ?? file;
        }

        private void RemovePayload()
        {
            var i = _list.SelectedIndex;
            if (i < 0 || Hekate.IsHekate((string)_list.Items[i])) return;
            _list.Items.RemoveAt(i);
            Favorites.Save(_list.Items.Cast<string>());
            if (_list.Items.Count > 0) _list.SelectedIndex = Math.Min(i, _list.Items.Count - 1);
            UpdateUi();
        }

        private void Log(string text) => Log(text, Theme.Muted);

        private void Log(string text, Color color)
        {
            _log.SelectionStart = _log.TextLength;
            _log.SelectionColor = Theme.Border;
            _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  ");
            _log.SelectionColor = color;
            _log.AppendText(text + Environment.NewLine);
            _log.ScrollToCaret();
        }
    }
}
