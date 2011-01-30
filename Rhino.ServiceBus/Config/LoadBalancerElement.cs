using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    public class LoadBalancerElement : ConfigurationElement 
    {
        public LoadBalancerElement()
        {
            SetupDefaults();
        }

        public int? ThreadCount
        {
            get { return (int?) this["threadCount"]; }
        }

        public string Endpoint
        {
            get { return this["endpoint"] as string; }
        }

        public string ReadForWorkEndpoint
        {
            get { return this["readyForWorkEndpoint"] as string; }
        }

        public string PrimaryLoadBalancerEndpoint
        {
            get { return this["primaryLoadBalancerEndpoint"] as string; }
        }

        public string SecondaryLoadBalancerEndpoint
        {
            get { return this["secondaryLoadBalancerEndpoint"] as string; }
        }

        private void SetupDefaults()
        {
            Properties.Add(new ConfigurationProperty("threadCount", typeof(int?), null));
            Properties.Add(new ConfigurationProperty("endpoint", typeof(string), null));
            Properties.Add(new ConfigurationProperty("readyForWorkEndpoint", typeof(string), null));
            Properties.Add(new ConfigurationProperty("primaryLoadBalancerEndpoint", typeof(string), null));
            Properties.Add(new ConfigurationProperty("secondaryLoadBalancerEndpoint", typeof(string), null));
        }
    }
}