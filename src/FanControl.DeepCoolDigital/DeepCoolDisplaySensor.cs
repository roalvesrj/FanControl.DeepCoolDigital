using FanControl.Plugins;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Exposes the temperature used for a cooler display as a FanControl sensor.
    /// </summary>
    /// <remarks>
    /// The value is refreshed by <see cref="DeepCoolDigitalPlugin.Update"/>; the sensor's own
    /// <see cref="Update"/> is intentionally empty. Values are always in degrees Celsius, regardless of
    /// the display unit configured for the device.
    /// </remarks>
    internal sealed class DeepCoolDisplaySensor : IPluginSensor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeepCoolDisplaySensor"/> class.
        /// </summary>
        /// <param name="id">The unique sensor id.</param>
        /// <param name="name">The sensor name shown in the FanControl UI.</param>
        public DeepCoolDisplaySensor(string id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Gets the unique sensor id.
        /// </summary>
        /// <value>The stable identifier used by FanControl.</value>
        public string Id { get; }

        /// <summary>
        /// Gets the sensor name shown in the FanControl UI.
        /// </summary>
        /// <value>The display name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the current temperature, in degrees Celsius.
        /// </summary>
        /// <value>The temperature, or <see langword="null" /> while no valid reading is available.</value>
        public float? Value { get; set; }

        /// <inheritdoc />
        public void Update()
        {
        }
    }
}
