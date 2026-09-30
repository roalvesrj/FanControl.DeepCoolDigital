namespace FanControl.DeepCoolDigital.Core.Protocols
{
    /// <summary>
    /// Carries the system state that a display packet should render.
    /// </summary>
    public readonly struct DisplayFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayFrame"/> struct.
        /// </summary>
        /// <param name="field">One of the enumeration values that specifies which value is shown on the main display.</param>
        /// <param name="value">The value to display, already converted to the unit expected by the device.</param>
        /// <param name="temperature">The current CPU temperature, in degrees Celsius, used for alerts.</param>
        /// <param name="usage">The current CPU usage percentage, used for secondary indicators such as bars.</param>
        /// <param name="alarm"><see langword="true" /> to raise the high-temperature alert; otherwise, <see langword="false" />.</param>
        public DisplayFrame(DisplayField field, float value, float temperature, float usage, bool alarm)
        {
            Field = field;
            Value = value;
            Temperature = temperature;
            Usage = usage;
            Alarm = alarm;
        }

        /// <summary>
        /// Gets which value is shown on the main display.
        /// </summary>
        /// <value>One of the enumeration values that specifies the displayed field.</value>
        public DisplayField Field { get; }

        /// <summary>
        /// Gets the value to display, in the unit expected by the device.
        /// </summary>
        /// <value>The main display value; matches <see cref="Temperature"/> or <see cref="Usage"/> depending on <see cref="Field"/>.</value>
        public float Value { get; }

        /// <summary>
        /// Gets the current CPU temperature, in degrees Celsius.
        /// </summary>
        /// <value>The temperature used for alert decisions.</value>
        public float Temperature { get; }

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        /// <value>The usage used for secondary indicators such as status bars.</value>
        public float Usage { get; }

        /// <summary>
        /// Gets a value indicating whether the high-temperature alert is active.
        /// </summary>
        /// <value><see langword="true" /> when the alert should be raised; otherwise, <see langword="false" />.</value>
        public bool Alarm { get; }
    }
}
