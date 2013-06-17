using System;
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
            set { this["threadCount"] = value; }
        }

        public string Endpoint
        {
            get { return this["endpoint"] as string; }
            set { this["endpoint"] = value; }
        }

        public int? NumberOfRetries
        {
            get { return (int?) this["numberOfRetries"]; }
            set { this["numberOfRetries"] = value; }
        }

        public string QueueIsolationLevel
        {
            get { return this["queueIsolationLevel"] as string; }
            set { this["queueIsolationLevel"] = value; }
        }

        public bool? ConsumeInTransaction
        {
            get { return (bool?) this["consumeInTransaction"]; }
        }

        public string Transactional
        {
            get { return this["transactional"] as string; }
            set { this["transactional"] = value; }
        }

        public string LogEndpoint
        {
            get { return this["logEndpoint"] as string; }
            set { this["logEndpoint"] = value; }
        }

        public string LoadBalancerEndpoint
        {
            get { return this["loadBalancerEndpoint"] as string; }
            set { this["loadBalancerEndpoint"] = value; }
        }

        public string Name
        {
            get { return this["name"] as string; }
            set { this["name"] = value; }
        }

        public string Path
        {
            get { return this["path"] as string; }
            set { this["path"] = value; }
        }
        
        private string BasePath
        {
            get
            {
                var basePath = (Path == null) ?
                    System.IO.Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory) :
                    Environment.ExpandEnvironmentVariables(Path); 
                
                //Due to validation checks elsewhere, path should never be null except in the cases of a one-way bus
                var folderName = Name == null || Name.Trim().Length == 0 ? "one_way" : Name;

                return System.IO.Path.Combine(basePath, folderName);
            }
        }

        public string QueuePath
        {
            get { return BasePath + ".esent"; }
        }

        public string SubscriptionPath
        {
            get { return BasePath + "_subscriptions.esent"; }
        }

        public bool EnablePerformanceCounters
        {
            get { return (bool)this["enablePerformanceCounters"]; }
            set { this["enablePerformanceCounters"] = value; }
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
            Properties.Add(new ConfigurationProperty("path", typeof(string), null));
            Properties.Add(new ConfigurationProperty("enablePerformanceCounters", typeof(bool), false));
        }
    }
}