using Rhino.ServiceBus.Config;

namespace Rhino.ServiceBus.Hosting
{
    public class RhinoQueuesHostConfiguration : HostConfiguration
    {
        private string Path { get; set; }

        public HostConfiguration StoragePath(string path)
        {
            Path = path;
            return this;
        }

        public override BusConfigurationSection ToBusConfiguration()
        {
            var config = base.ToBusConfiguration();

            if (string.IsNullOrEmpty(Path) == false)
                config.Bus.Path = Path;

            return config;
        }
    }
}