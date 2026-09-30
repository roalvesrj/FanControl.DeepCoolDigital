using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Represents the plugin configuration loaded from the <c>DeepCoolDigital.ini</c> file.
    /// </summary>
    /// <remarks>
    /// The file is re-read automatically: call <see cref="TryReload"/> from the plugin update loop and
    /// changes are applied without restarting FanControl.
    /// </remarks>
    public sealed class PluginConfig
    {
        /// <summary>
        /// The default USB vendor id used when none is configured.
        /// </summary>
        public const int DefaultVendorId = 0x3633;

        /// <summary>
        /// The default USB product id used when none is configured.
        /// </summary>
        public const int DefaultProductId = 0x0008;

        /// <summary>
        /// The default list of preferred CPU temperature sensor names, in priority order.
        /// </summary>
        public static readonly string[] DefaultPreferredTemperatureSensors =
        {
            "CPU Package",
            "Core (Tctl/Tdie)",
            "Core (Tctl)",
            "Core (Tdie)",
            "CPU (Tctl/Tdie)",
            "Core Average"
        };

        private DateTime _loadedWriteUtc = DateTime.MinValue;

        /// <summary>
        /// Gets the USB vendor id of the display.
        /// </summary>
        /// <value>The configured vendor id. The default is <see cref="DefaultVendorId"/>.</value>
        public int VendorId { get; private set; } = DefaultVendorId;

        /// <summary>
        /// Gets the USB product id of the display.
        /// </summary>
        /// <value>The configured product id. The default is <see cref="DefaultProductId"/>.</value>
        public int ProductId { get; private set; } = DefaultProductId;

        /// <summary>
        /// Gets what the cooler display shows.
        /// </summary>
        /// <value>One of the enumeration values that specifies the display mode. The default is <see cref="DisplayMode.Temperature"/>.</value>
        public DisplayMode Mode { get; private set; } = DisplayMode.Temperature;

        /// <summary>
        /// Gets the number of seconds between temperature and usage when <see cref="Mode"/> is <see cref="DisplayMode.Auto"/>.
        /// </summary>
        /// <value>The alternation interval, in seconds. The default is <c>5</c>.</value>
        public int AutoSwitchSeconds { get; private set; } = 5;

        /// <summary>
        /// Gets the threshold above which the high-temperature alert is raised.
        /// </summary>
        /// <value>The alert threshold, in degrees Celsius. The default is <c>90</c>.</value>
        public float AlarmTemperature { get; private set; } = 90f;

        /// <summary>
        /// Gets how much information the plugin writes to its log file.
        /// </summary>
        /// <value>One of the enumeration values that specifies the log level. The default is <see cref="LogLevel.Off"/>.</value>
        public LogLevel LogLevel { get; private set; } = LogLevel.Off;

        /// <summary>
        /// Gets the preferred CPU temperature sensor names, in priority order.
        /// </summary>
        /// <value>The configured sensor names. The default is <see cref="DefaultPreferredTemperatureSensors"/>.</value>
        public string[] PreferredTemperatureSensors { get; private set; } = DefaultPreferredTemperatureSensors;

        /// <summary>
        /// Gets the full path of the configuration file this instance was loaded from.
        /// </summary>
        /// <value>The absolute path of the ini file.</value>
        public string FilePath { get; private set; }

        /// <summary>
        /// Loads the configuration from the specified file.
        /// </summary>
        /// <param name="filePath">The full path of the ini file.</param>
        /// <returns>A configuration instance; defaults are used for missing or invalid entries.</returns>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> is <see langword="null" />, empty or whitespace.</exception>
        public static PluginConfig Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A configuration file path is required.", nameof(filePath));
            }

            var config = new PluginConfig { FilePath = filePath };

            try
            {
                config._loadedWriteUtc = File.Exists(filePath)
                    ? File.GetLastWriteTimeUtc(filePath)
                    : DateTime.MinValue;

                if (!File.Exists(filePath))
                {
                    return config;
                }

                bool logLevelSpecified = false;
                bool legacyLogEnabled = false;

                foreach (string rawLine in File.ReadAllLines(filePath))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                    {
                        continue;
                    }

                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

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
                        case "loglevel":
                            config.LogLevel = ParseLogLevel(value, config.LogLevel);
                            logLevelSpecified = true;
                            break;
                        case "log":
                            legacyLogEnabled = ParseBool(value, false);
                            break;
                        case "preferredtempsensors":
                            config.PreferredTemperatureSensors = ParseSensorNames(value, config.PreferredTemperatureSensors);
                            break;
                    }
                }

                if (!logLevelSpecified && legacyLogEnabled)
                {
                    config.LogLevel = LogLevel.Events;
                }
            }
            catch
            {
                // A malformed configuration must never prevent the plugin from loading; defaults apply.
            }

            return config;
        }

        /// <summary>
        /// Re-reads the configuration file when it changed on disk.
        /// </summary>
        /// <returns><see langword="true" /> when the file was re-read and at least one setting changed; otherwise, <see langword="false" />.</returns>
        public bool TryReload()
        {
            try
            {
                DateTime writeUtc = File.Exists(FilePath)
                    ? File.GetLastWriteTimeUtc(FilePath)
                    : DateTime.MinValue;

                if (writeUtc == _loadedWriteUtc)
                {
                    return false;
                }

                var reloaded = Load(FilePath);
                _loadedWriteUtc = writeUtc;

                bool changed =
                    reloaded.Mode != Mode ||
                    reloaded.AutoSwitchSeconds != AutoSwitchSeconds ||
                    reloaded.AlarmTemperature != AlarmTemperature ||
                    reloaded.VendorId != VendorId ||
                    reloaded.ProductId != ProductId ||
                    reloaded.LogLevel != LogLevel ||
                    !reloaded.PreferredTemperatureSensors.SequenceEqual(PreferredTemperatureSensors);

                CopyFrom(reloaded);
                return changed;
            }
            catch
            {
                return false;
            }
        }

        private void CopyFrom(PluginConfig other)
        {
            Mode = other.Mode;
            AutoSwitchSeconds = other.AutoSwitchSeconds;
            AlarmTemperature = other.AlarmTemperature;
            VendorId = other.VendorId;
            ProductId = other.ProductId;
            LogLevel = other.LogLevel;
            PreferredTemperatureSensors = other.PreferredTemperatureSensors;
        }

        private static DisplayMode ParseMode(string value, DisplayMode fallback)
        {
            switch (value.ToLowerInvariant())
            {
                case "auto":
                case "dynamic":
                case "both":
                    return DisplayMode.Auto;
                case "temp":
                case "temperature":
                    return DisplayMode.Temperature;
                case "usage":
                case "load":
                    return DisplayMode.Usage;
                default:
                    return fallback;
            }
        }

        private static LogLevel ParseLogLevel(string value, LogLevel fallback)
        {
            switch (value.ToLowerInvariant())
            {
                case "off":
                case "none":
                case "false":
                case "0":
                    return LogLevel.Off;
                case "events":
                case "event":
                case "on":
                case "true":
                case "1":
                    return LogLevel.Events;
                case "verbose":
                case "debug":
                    return LogLevel.Verbose;
                default:
                    return fallback;
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
                case "on":
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    return false;
                default:
                    return fallback;
            }
        }

        private static string[] ParseSensorNames(string value, string[] fallback)
        {
            string[] names = value
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .Where(name => name.Length > 0)
                .ToArray();

            return names.Length > 0 ? names : fallback;
        }
    }
}
