using System.Collections.Generic;

namespace RcmGui
{
    /// <summary>Texte auf Deutsch und Englisch. T(key) gibt den Text in der aktuellen Sprache.</summary>
    internal static class Strings
    {
        private static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>
        {
            // key = { de, en }
            ["payloads"] = new[] { "Payloads", "Payloads" },
            ["selected"] = new[] { "Ausgewählt", "Selected" },
            ["log"] = new[] { "Protokoll", "Log" },
            ["add"] = new[] { "Hinzufügen", "Add" },
            ["remove"] = new[] { "Entfernen", "Remove" },
            ["inject"] = new[] { "Payload senden", "Send payload" },
            ["injecting"] = new[] { "Sende ...", "Sending ..." },
            ["driver"] = new[] { "Treiber-Hilfe", "Driver help" },
            ["noPayload"] = new[] { "Kein Payload", "No payload" },
            ["settings"] = new[] { "Einstellungen", "Settings" },
            ["autoInject"] = new[] { "Automatisch senden", "Auto inject" },
            ["autoInjectHint"] = new[] { "Sendet den gewählten Payload automatisch, sobald die Switch bereit ist.", "Sends the selected payload automatically once the Switch is ready." },
            ["language"] = new[] { "Sprache", "Language" },
            ["theme"] = new[] { "Design", "Theme" },
            ["themeDark"] = new[] { "Dunkel", "Dark" },
            ["themeLight"] = new[] { "Hell", "Light" },
            ["close"] = new[] { "Schließen", "Close" },
            ["tipInject"] = new[] { "Ausgewählten Payload an die Switch senden", "Send the selected payload to the Switch" },
            ["tipDriver"] = new[] { "Hilfe zum libusbK-Treiber (Zadig)", "Help for the libusbK driver (Zadig)" },
            ["tipAdd"] = new[] { "Payload-Datei (.bin) hinzufügen. Geht auch per Ziehen ins Fenster", "Add a payload file (.bin). Drag and drop works too" },
            ["tipRemove"] = new[] { "Ausgewählten Payload aus der Liste entfernen", "Remove the selected payload from the list" },
            ["tipStatus"] = new[] { "Status der USB-Verbindung zur Switch", "Status of the USB connection to the Switch" },
            ["tipSettings"] = new[] { "Einstellungen", "Settings" },
            ["stReady"] = new[] { "Switch bereit (RCM)", "Switch ready (RCM)" },
            ["stNoDriver"] = new[] { "Treiber fehlt", "Driver missing" },
            ["stNone"] = new[] { "Keine Switch", "No Switch" },
            ["hekateAuto"] = new[] { "Neueste Version, automatisch geladen", "Latest version, downloaded automatically" },
            ["drvAskTitle"] = new[] { "Treiber fehlt", "Driver missing" },
            ["drvAsk"] = new[] { "Der Treiber für die Switch (libusbK) ist nicht installiert. Jetzt installieren?", "The Switch driver (libusbK) is not installed. Install it now?" },
            ["drvInstalling"] = new[] { "Installiere libusbK-Treiber ...", "Installing libusbK driver ..." },
            ["drvOk"] = new[] { "Treiber installiert.", "Driver installed." },
            ["drvCancel"] = new[] { "Treiberinstallation abgebrochen.", "Driver installation cancelled." },
            ["drvFail"] = new[] { "Treiber konnte nicht installiert werden.", "Driver could not be installed." },
        };

        public static string T(string key)
        {
            if (!Map.TryGetValue(key, out var v)) return key;
            return Settings.Lang == "en" ? v[1] : v[0];
        }
    }
}
