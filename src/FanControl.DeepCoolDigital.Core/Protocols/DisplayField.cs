namespace FanControl.DeepCoolDigital.Core.Protocols
{
    /// <summary>
    /// Specifies which value a display packet carries.
    /// </summary>
    public enum DisplayField
    {
        /// <summary>
        /// CPU temperature, in the unit expected by the device.
        /// </summary>
        Temperature,

        /// <summary>
        /// CPU usage percentage.
        /// </summary>
        Usage
    }
}
