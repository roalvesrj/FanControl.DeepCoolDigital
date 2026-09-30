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
    /// Global keys apply to every device; optional <c>[device:VID:PID]</c> sections override individual
    /// settings (mode, interval, alert and unit) for one USB identity. The file is re-read automatically:
    /// call <see cref="TryReload"/> from the plugin update loop and changes are applied without restarting
    /// FanControl.
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

        private static readonly string[] DefaultSensorNames =
        {
            "CPU Package",
            "Core (Tctl/Tdie)",
            "Core (Tctl)",
            "Core (Tdie)",
            "CPU (Tctl/Tdie)",
            "Core Average"
        };

        /// <summary>
        /// Gets the default list of preferred CPU temperature sensor names, in priority order.
        /// </summary>
        /// <value>A read-only list of the built-in sensor names.</value>
        public static IReadOnlyList<string> DefaultPreferredTemperatureSensors => Array.AsReadOnly(DefaultSensorNames);

        private readonly List<DeviceOverride> _deviceOverrides = new List<DeviceOverride>();
        private string[] _preferredTemperatureSensors = DefaultSensorNames;
        private DateTime _loadedWriteUtc = DateTime.MinValue;
        private bool _loadedSuccessfully;

        /// <summary>
        /// Gets the USB vendor id of the primary display target.
        /// </summary>
        /// <value>The configured vendor id. The default is <see cref="DefaultVendorId"/>.</value>
        public int VendorId { get; private set; } = DefaultVendorId;

        /// <summary>
        /// Gets the USB product id of the primary display target.
        /// </summary>
        /// <value>The configured product id. The default is <see cref="DefaultProductId"/>.</value>
        public int ProductId { get; private set; } = DefaultProductId;

        /// <summary>
        /// Gets what the cooler displays show.
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
        /// Gets a value indicating whether the high-temperature alert is allowed.
        /// </summary>
        /// <value><see langword="true" /> when the alert is allowed; otherwise, <see langword="false" />. The default is <see langword="true" />.</value>
        public bool AlarmEnabled { get; private set; } = true;

        /// <summary>
        /// Gets a value indicating whether temperatures are displayed in Fahrenheit.
        /// </summary>
        /// <value><see langword="true" /> when Fahrenheit is used; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        public bool Fahrenheit { get; private set; }

        /// <summary>
        /// Gets how much information the plugin writes to its log file.
        /// </summary>
        /// <value>One of the enumeration values that specifies the log level. The default is <see cref="LogLevel.Off"/>.</value>
        public LogLevel LogLevel { get; private set; } = LogLevel.Off;

        /// <summary>
        /// Gets the preferred CPU temperature sensor names, in priority order.
        /// </summary>
        /// <value>The configured sensor names. The default is <see cref="DefaultPreferredTemperatureSensors"/>.</value>
        public IReadOnlyList<string> PreferredTemperatureSensors => Array.AsReadOnly(_preferredTemperatureSensors);

        /// <summary>
        /// Gets the per-device overrides parsed from <c>[device:VID:PID]</c> sections.
        /// </summary>
        /// <value>A read-only list of the configured overrides; empty when the file defines none.</value>
        public IReadOnlyList<DeviceOverride> DeviceOverrides => _deviceOverrides.AsReadOnly();

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
                if (!File.Exists(filePath))
                {
                    config._loadedSuccessfully = true;
                    return config;
                }

                string[] lines = File.ReadAllLines(filePath);
                config._loadedWriteUtc = File.GetLastWriteTimeUtc(filePath);
                config._loadedSuccessfully = true;

                bool logLevelSpecified = false;
                bool legacyLogEnabled = false;
                var sections = new Dictionary<(int VendorId, int ProductId), DeviceOverrideBuilder>();
                DeviceOverrideBuilder currentSection = null;
                bool inInvalidSection = false;

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                    {
                        continue;
                    }

                    if (line.StartsWith("["))
                    {
                        if (TryParseSectionHeader(line, out int sectionVendor, out int sectionProduct))
                        {
                            currentSection = GetOrAddSection(sections, sectionVendor, sectionProduct);
                            inInvalidSection = false;
                        }
                        else
                        {
                            currentSection = null;
                            inInvalidSection = true;
                        }

                        continue;
                    }

                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    if (inInvalidSection)
                    {
                        continue;
                    }

                    string key = line.Substring(0, separator).Trim().ToLowerInvariant();
                    string value = line.Substring(separator + 1).Trim();

                    if (currentSection != null)
                    {
                        ApplySectionKey(currentSection, key, value);
                        continue;
                    }

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
                        case "alarmenabled":
                            config.AlarmEnabled = ParseBool(value, config.AlarmEnabled);
                            break;
                        case "fahrenheit":
                            config.Fahrenheit = ParseBool(value, config.Fahrenheit);
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
                            config._preferredTemperatureSensors = ParseSensorNames(value, config._preferredTemperatureSensors);
                            break;
                    }
                }

                if (!logLevelSpecified && legacyLogEnabled)
                {
                    config.LogLevel = LogLevel.Events;
                }

                foreach (DeviceOverrideBuilder builder in sections.Values)
                {
                    config._deviceOverrides.Add(builder.Build());
                }
            }
            catch
            {
                // A transient I/O failure leaves _loadedSuccessfully false and the timestamp unlatched,
                // so TryReload retries on the next cycle. Malformed values never throw: each key falls
                // back to its default during parsing.
            }

            return config;
        }

        /// <summary>
        /// Gets the effective settings of a device by merging global values and its override section.
        /// </summary>
        /// <param name="vendorId">The USB vendor id of the device.</param>
        /// <param name="productId">The USB product id of the device.</param>
        /// <returns>The merged settings for the identity.</returns>
        public DeviceSettings ForDevice(int vendorId, int productId)
        {
            DisplayMode mode = Mode;
            int autoSwitchSeconds = AutoSwitchSeconds;
            float alarmTemperature = AlarmTemperature;
            bool alarmEnabled = AlarmEnabled;
            bool fahrenheit = Fahrenheit;

            foreach (DeviceOverride deviceOverride in _deviceOverrides)
            {
                if (deviceOverride.VendorId != vendorId || deviceOverride.ProductId != productId)
                {
                    continue;
                }

                if (deviceOverride.Mode.HasValue) mode = deviceOverride.Mode.Value;
                if (deviceOverride.AutoSwitchSeconds.HasValue) autoSwitchSeconds = deviceOverride.AutoSwitchSeconds.Value;
                if (deviceOverride.AlarmTemperature.HasValue) alarmTemperature = deviceOverride.AlarmTemperature.Value;
                if (deviceOverride.AlarmEnabled.HasValue) alarmEnabled = deviceOverride.AlarmEnabled.Value;
                if (deviceOverride.Fahrenheit.HasValue) fahrenheit = deviceOverride.Fahrenheit.Value;
                break;
            }

            return new DeviceSettings(vendorId, productId, mode, autoSwitchSeconds, alarmTemperature, alarmEnabled, fahrenheit);
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
                if (!reloaded._loadedSuccessfully)
                {
                    return false;
                }

                _loadedWriteUtc = reloaded._loadedWriteUtc;

                bool changed =
                    reloaded.Mode != Mode ||
                    reloaded.AutoSwitchSeconds != AutoSwitchSeconds ||
                    reloaded.AlarmTemperature != AlarmTemperature ||
                    reloaded.AlarmEnabled != AlarmEnabled ||
                    reloaded.Fahrenheit != Fahrenheit ||
                    reloaded.VendorId != VendorId ||
                    reloaded.ProductId != ProductId ||
                    reloaded.LogLevel != LogLevel ||
                    !reloaded.PreferredTemperatureSensors.SequenceEqual(PreferredTemperatureSensors) ||
                    !DeviceOverridesEqual(reloaded._deviceOverrides, _deviceOverrides);

                CopyFrom(reloaded);
                return changed;
            }
            catch
            {
                return false;
            }
        }

        private static bool DeviceOverridesEqual(IReadOnlyList<DeviceOverride> left, IReadOnlyList<DeviceOverride> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Count; index++)
            {
                DeviceOverride first = left[index];
                DeviceOverride second = right[index];

                if (first.VendorId != second.VendorId ||
                    first.ProductId != second.ProductId ||
                    first.Mode != second.Mode ||
                    first.AutoSwitchSeconds != second.AutoSwitchSeconds ||
                    first.AlarmTemperature != second.AlarmTemperature ||
                    first.AlarmEnabled != second.AlarmEnabled ||
                    first.Fahrenheit != second.Fahrenheit)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ApplySectionKey(DeviceOverrideBuilder section, string key, string value)
        {
            switch (key)
            {
                case "mode":
                    section.Mode = ParseMode(value, section.Mode ?? DisplayMode.Temperature);
                    break;
                case "autoswitchseconds":
                    section.AutoSwitchSeconds = Math.Max(1, ParseInt(value, section.AutoSwitchSeconds ?? 5));
                    break;
                case "alarmtemperature":
                    section.AlarmTemperature = Math.Max(1f, ParseFloat(value, section.AlarmTemperature ?? 90f));
                    break;
                case "alarmenabled":
                    section.AlarmEnabled = ParseBool(value, section.AlarmEnabled ?? true);
                    break;
                case "fahrenheit":
                    section.Fahrenheit = ParseBool(value, section.Fahrenheit ?? false);
                    break;
            }
        }

        private static DeviceOverrideBuilder GetOrAddSection(
            Dictionary<(int VendorId, int ProductId), DeviceOverrideBuilder> sections,
            int vendorId,
            int productId)
        {
            if (!sections.TryGetValue((vendorId, productId), out DeviceOverrideBuilder section))
            {
                section = new DeviceOverrideBuilder(vendorId, productId);
                sections.Add((vendorId, productId), section);
            }

            return section;
        }

        private static bool TryParseSectionHeader(string line, out int vendorId, out int productId)
        {
            vendorId = 0;
            productId = 0;

            if (line.Length < 2 || line[line.Length - 1] != ']')
            {
                return false;
            }

            string inner = line.Substring(1, line.Length - 2).Trim();
            string[] parts = inner.Split(':');

            if (parts.Length != 3 || !parts[0].Trim().Equals("device", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int parsedVendor = ParseInt(parts[1].Trim(), int.MinValue);
            int parsedProduct = ParseInt(parts[2].Trim(), int.MinValue);

            if (parsedVendor == int.MinValue || parsedProduct == int.MinValue)
            {
                return false;
            }

            vendorId = parsedVendor;
            productId = parsedProduct;
            return true;
        }

        private void CopyFrom(PluginConfig other)
        {
            Mode = other.Mode;
            AutoSwitchSeconds = other.AutoSwitchSeconds;
            AlarmTemperature = other.AlarmTemperature;
            AlarmEnabled = other.AlarmEnabled;
            Fahrenheit = other.Fahrenheit;
            VendorId = other.VendorId;
            ProductId = other.ProductId;
            LogLevel = other.LogLevel;
            _preferredTemperatureSensors = other._preferredTemperatureSensors;

            _deviceOverrides.Clear();
            _deviceOverrides.AddRange(other._deviceOverrides);
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

        private sealed class DeviceOverrideBuilder
        {
            public DeviceOverrideBuilder(int vendorId, int productId)
            {
                VendorId = vendorId;
                ProductId = productId;
            }

            public int VendorId { get; }

            public int ProductId { get; }

            public DisplayMode? Mode { get; set; }

            public int? AutoSwitchSeconds { get; set; }

            public float? AlarmTemperature { get; set; }

            public bool? AlarmEnabled { get; set; }

            public bool? Fahrenheit { get; set; }

            public DeviceOverride Build()
            {
                return new DeviceOverride(VendorId, ProductId, Mode, AutoSwitchSeconds, AlarmTemperature, AlarmEnabled, Fahrenheit);
            }
        }
    }
}
