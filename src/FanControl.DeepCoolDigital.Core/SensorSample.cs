namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Represents a named temperature reading from a hardware source.
    /// </summary>
    public readonly struct SensorSample
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SensorSample"/> struct.
        /// </summary>
        /// <param name="name">The display name of the sensor.</param>
        /// <param name="value">The temperature value, in degrees Celsius.</param>
        public SensorSample(string name, float value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets the display name of the sensor.
        /// </summary>
        /// <value>The sensor name, for example "CPU Package".</value>
        public string Name { get; }

        /// <summary>
        /// Gets the temperature value, in degrees Celsius.
        /// </summary>
        /// <value>The current temperature.</value>
        public float Value { get; }
    }
}
