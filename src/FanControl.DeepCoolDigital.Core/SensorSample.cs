namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Represents a named sensor reading from a hardware or software source.
    /// </summary>
    public readonly struct SensorSample
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SensorSample"/> struct without an identifier.
        /// </summary>
        /// <param name="name">The display name of the sensor.</param>
        /// <param name="value">The sensor value.</param>
        public SensorSample(string name, float value)
            : this(name, value, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorSample"/> struct.
        /// </summary>
        /// <param name="name">The display name of the sensor.</param>
        /// <param name="value">The sensor value.</param>
        /// <param name="identifier">The optional stable identifier of the sensor.</param>
        public SensorSample(string name, float value, string identifier)
        {
            Name = name;
            Value = value;
            Identifier = identifier;
        }

        /// <summary>
        /// Gets the display name of the sensor.
        /// </summary>
        /// <value>The sensor name, for example "CPU Package".</value>
        public string Name { get; }

        /// <summary>
        /// Gets the sensor value.
        /// </summary>
        /// <value>The current reading; the unit depends on the sensor type.</value>
        public float Value { get; }

        /// <summary>
        /// Gets the optional stable identifier of the sensor.
        /// </summary>
        /// <value>The identifier used by the source, or <see langword="null" /> when the source does not provide one.</value>
        public string Identifier { get; }
    }
}
