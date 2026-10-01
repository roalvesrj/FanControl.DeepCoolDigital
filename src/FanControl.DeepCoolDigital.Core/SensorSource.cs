namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Specifies where the CPU temperature and usage readings come from.
    /// </summary>
    public enum SensorSource
    {
        /// <summary>
        /// Prefers FanControl's own sensors through its IPC channel and falls back to the local
        /// LibreHardwareMonitor/kernel sources when unavailable.
        /// </summary>
        Auto,

        /// <summary>
        /// Uses FanControl's own sensors through its IPC channel only.
        /// </summary>
        FanControl,

        /// <summary>
        /// Uses the local LibreHardwareMonitor instance and Windows kernel counters only.
        /// </summary>
        Local
    }
}
