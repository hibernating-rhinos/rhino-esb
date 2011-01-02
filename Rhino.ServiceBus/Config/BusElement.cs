using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    public class BusElement : ConfigurationElement 
    {
        public BusElement()
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

        public int? NumberOfRetries
        {
            get { return (int?) this["numberOfRetries"]; }
        }

        public string QueueIsolationLevel
        {
            get { return this["queueIsolationLevel"] as string; }
        }

        public bool? ConsumeInTransaction
        {
            get { return (bool?) this["consumeInTransaction"]; }
        }

        public string Transactional
        {
            get { return this["transactional"] as string; }
        }

        public string LogEndpoint
        {
            get { return this["logEndpoint"] as string; }
        }

        public string LoadBalancerEndpoint
        {
            get { return this["loadBalancerEndpoint"] as string; }
        }

        public string Name
        {
            get { return this["name"] as string; }
        }

        private void SetupDefaults()
        {
            Properties.Add(new ConfigurationProperty("transactional", typeof(string), null));
            Properties.Add(new ConfigurationProperty("threadCount", typeof(int?), null));
            Properties.Add(new ConfigurationProperty("endpoint", typeof(string), null));
            Properties.Add(new ConfigurationProperty("numberOfRetries", typeof(int?), null));
            Properties.Add(new ConfigurationProperty("queueIsolationLevel", typeof(string), null));
            Properties.Add(new ConfigurationProperty("consumeInTransaction", typeof(bool?), null));
            Properties.Add(new ConfigurationProperty("logEndpoint", typeof(string), null));
            Properties.Add(new ConfigurationProperty("loadBalancerEndpoint", typeof(string), null));
            Properties.Add(new ConfigurationProperty("name", typeof(string), null));
        }
    }
}