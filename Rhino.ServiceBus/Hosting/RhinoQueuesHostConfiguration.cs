using Rhino.ServiceBus.Config;

namespace Rhino.ServiceBus.Hosting
{
    public class RhinoQueuesHostConfiguration : HostConfiguration
    {
        private string path;
        private bool enablePerformanceCounters;

        public RhinoQueuesHostConfiguration()
        {
            enablePerformanceCounters = false;
        }

        public RhinoQueuesHostConfiguration StoragePath(string path)
        {
            this.path = path;
            return this;
        }

        public RhinoQueuesHostConfiguration EnablePerformanceCounters()
        {
            enablePerformanceCounters = true;
            return this;
        }

        public override BusConfigurationSection ToBusConfiguration()
        {
            var config = base.ToBusConfiguration();

            if (string.IsNullOrEmpty(path) == false)
                config.Bus.Path = path;

            config.Bus.EnablePerformanceCounters = enablePerformanceCounters;

            return config;
        
        }
    }
}



