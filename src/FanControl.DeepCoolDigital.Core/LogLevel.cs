namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Specifies how much information the plugin writes to its log file.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Disables logging.
        /// </summary>
        Off,

        /// <summary>
        /// Logs connection, configuration and error events.
        /// </summary>
        Events,

        /// <summary>
        /// Logs events plus diagnostic details such as discovered sensors.
        /// </summary>
        Verbose
    }
}
