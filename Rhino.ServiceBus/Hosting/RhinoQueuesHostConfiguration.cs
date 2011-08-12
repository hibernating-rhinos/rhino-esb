using Rhino.ServiceBus.Config;

namespace Rhino.ServiceBus.Hosting
{
    public class RhinoQueuesHostConfiguration : HostConfiguration
    {
        private bool enablePerformanceCounters;

        public RhinoQueuesHostConfiguration()
        {
            enablePerformanceCounters = false;
        }

        public RhinoQueuesHostConfiguration EnablePerformanceCounters()
        {
            enablePerformanceCounters = true;
            return this;
        }

        public override BusConfigurationSection ToBusConfiguration()
        {
            var config = base.ToBusConfiguration();

            config.Bus.EnablePerformanceCounters = enablePerformanceCounters;

            return config;
        
        }
    }
}



