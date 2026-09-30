using FanControl.Plugins;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Exposes the temperature used for the cooler display as a FanControl sensor.
    /// </summary>
    /// <remarks>
    /// The value is refreshed by <see cref="DeepCoolDigitalPlugin.Update"/>; the sensor's own
    /// <see cref="Update"/> is intentionally empty.
    /// </remarks>
    internal sealed class DeepCoolDisplaySensor : IPluginSensor
    {
        /// <summary>
        /// Gets the unique sensor id.
        /// </summary>
        /// <value>The stable identifier used by FanControl.</value>
        public string Id => "DeepCoolDigital/CpuTemperature";

        /// <summary>
        /// Gets the sensor name shown in the FanControl UI.
        /// </summary>
        /// <value>The display name.</value>
        public string Name => "DeepCool Display CPU Temp";

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
