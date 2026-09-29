using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace RcmGui
{
    /// <summary>Anleitung fuer den libusbK-Treiber (einmalig pro PC noetig).</summary>
    internal sealed class DriverHelpForm : Form
    {
        private const string Steps =
            "1.  Switch in den RCM-Modus bringen und per USB verbinden.\n\n" +
            "2.  Zadig öffnen (Button unten) und herunterladen.\n\n" +
            "3.  In Zadig das Gerät „APX“ wählen. Fehlt es: Options → List All Devices.\n\n" +
            "4.  Mit den Pfeilen libusbK (v3.0.7.0) als Treiber wählen. Wichtig!\n\n" +
            "5.  „Install Driver“ klicken. Danach zeigt diese App „Switch bereit“.";

        public DriverHelpForm()
        {
            Text = "Treiber-Hilfe";
            Icon = Theme.AppIcon();
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(520, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Padding = new Padding(16);

            var card = new Card { Dock = DockStyle.Fill };
            var text = new Label { Text = Steps, Dock = DockStyle.Fill, ForeColor = Theme.Text };
            card.Controls.Add(text);
            card.Controls.Add(new SectionLabel("libusbK-Treiber einrichten"));

            var buttons = new TableLayoutPanel { Dock = DockStyle.Bottom, ColumnCount = 3, Height = 50, Padding = new Padding(0, 14, 0, 0) };
            for (var i = 0; i < 3; i++) buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            var zadig = Theme.PrimaryButton("Zadig öffnen");
            var devmgr = Theme.SecondaryButton("Geräte-Manager");
            var close = Theme.SecondaryButton("Schließen");
            foreach (var b in new[] { zadig, devmgr, close }) { b.Dock = DockStyle.Fill; b.Margin = new Padding(3, 0, 3, 0); }
            buttons.Controls.Add(zadig, 0, 0);
            buttons.Controls.Add(devmgr, 1, 0);
            buttons.Controls.Add(close, 2, 0);

            zadig.Click += (s, e) => Open("https://zadig.akeo.ie/");
            devmgr.Click += (s, e) => Open("devmgmt.msc");
            close.Click += (s, e) => Close();
            CancelButton = close;

            Controls.Add(card);
            Controls.Add(buttons);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyTitleBar(Handle);
        }

        private static void Open(string target)
        {
            try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
            catch { /* nichts zu tun */ }
        }
    }
}
