using System.Collections.Generic;
using Castle.Core.Configuration;

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
        protected override void PopulateBusConfiguration(MutableConfiguration busConfig)
        {
            base.PopulateBusConfiguration(busConfig);

            if (string.IsNullOrEmpty(Path) == false)
                busConfig.Attribute("path", Path);
        }
    }
}