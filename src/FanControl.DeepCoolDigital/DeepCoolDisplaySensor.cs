using FanControl.Plugins;

namespace FanControl.DeepCoolDigital
{
    internal sealed class DeepCoolDisplaySensor : IPluginSensor
    {
        public string Id => "DeepCoolDigital/CpuTemperature";

        public string Name => "DeepCool Display CPU Temp";

        public float? Value { get; set; }

        public void Update()
        {
        }
    }
}
