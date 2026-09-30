using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace FanControl.DeepCoolDigital
{
    internal enum DisplayMode
    {
        Auto,
        Temperature,
        Usage
    }

    internal sealed class PluginConfig
    {
        public int VendorId = 0x3633;
        public int ProductId = 0x0008;
        public DisplayMode Mode = DisplayMode.Temperature;
        public int AutoSwitchSeconds = 5;
        public float AlarmTemperature = 90f;
        public bool LogEnabled;

        private DateTime _loadedWriteUtc = DateTime.MinValue;

        public static string BaseDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static string ConfigPath => Path.Combine(BaseDirectory, "DeepCoolDigital.ini");

        public static PluginConfig Load()
        {
            var config = new PluginConfig();

            try
            {
                config._loadedWriteUtc = File.Exists(ConfigPath)
                    ? File.GetLastWriteTimeUtc(ConfigPath)
                    : DateTime.MinValue;

                if (!File.Exists(ConfigPath)) return config;

                foreach (string rawLine in File.ReadAllLines(ConfigPath))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;

                    int separator = line.IndexOf('=');
                    if (separator <= 0) continue;

                    string key = line.Substring(0, separator).Trim().ToLowerInvariant();
                    string value = line.Substring(separator + 1).Trim();

                    switch (key)
                    {
                        case "mode":
                            config.Mode = ParseMode(value, config.Mode);
                            break;
                        case "autoswitchseconds":
                            config.AutoSwitchSeconds = Math.Max(1, ParseInt(value, config.AutoSwitchSeconds));
                            break;
                        case "alarmtemperature":
                            config.AlarmTemperature = Math.Max(1f, ParseFloat(value, config.AlarmTemperature));
                            break;
                        case "vendorid":
                            config.VendorId = ParseInt(value, config.VendorId);
                            break;
                        case "productid":
                            config.ProductId = ParseInt(value, config.ProductId);
                            break;
                        case "log":
                            config.LogEnabled = ParseBool(value, config.LogEnabled);
                            break;
                    }
                }
            }
            catch
            {
            }

            return config;
        }

        public bool TryReload()
        {
            try
            {
                DateTime writeUtc = File.Exists(ConfigPath)
                    ? File.GetLastWriteTimeUtc(ConfigPath)
                    : DateTime.MinValue;

                if (writeUtc == _loadedWriteUtc) return false;

                var reloaded = Load();
                _loadedWriteUtc = writeUtc;

                bool changed =
                    reloaded.Mode != Mode ||
                    reloaded.AutoSwitchSeconds != AutoSwitchSeconds ||
                    reloaded.AlarmTemperature != AlarmTemperature ||
                    reloaded.VendorId != VendorId ||
                    reloaded.ProductId != ProductId ||
                    reloaded.LogEnabled != LogEnabled;

                Mode = reloaded.Mode;
                AutoSwitchSeconds = reloaded.AutoSwitchSeconds;
                AlarmTemperature = reloaded.AlarmTemperature;
                VendorId = reloaded.VendorId;
                ProductId = reloaded.ProductId;
                LogEnabled = reloaded.LogEnabled;

                return changed;
            }
            catch
            {
                return false;
            }
        }

        private static DisplayMode ParseMode(string value, DisplayMode fallback)
        {
            switch (value.ToLowerInvariant())
            {
                case "auto":
                case "dynamic":
                case "both": return DisplayMode.Auto;
                case "temp":
                case "temperature": return DisplayMode.Temperature;
                case "usage":
                case "load": return DisplayMode.Usage;
                default: return fallback;
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex)
                    ? hex
                    : fallback;
            }

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                ? result
                : fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            switch (value.ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on": return true;
                case "0":
                case "false":
                case "no":
                case "off": return false;
                default: return fallback;
            }
        }
    }
}
