namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Specifies what the cooler display shows.
    /// </summary>
    public enum DisplayMode
    {
        /// <summary>
        /// Alternates between <see cref="Temperature"/> and <see cref="Usage"/>.
        /// </summary>
        Auto,

        /// <summary>
        /// Shows the CPU temperature only.
        /// </summary>
        Temperature,

        /// <summary>
        /// Shows the CPU usage only.
        /// </summary>
        Usage
    }
}
