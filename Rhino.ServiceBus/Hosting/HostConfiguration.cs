using System.Collections.Generic;
using System.Transactions;
using Rhino.ServiceBus.Config;
using System.Reflection;

namespace Rhino.ServiceBus.Hosting
{
    public class HostConfiguration
    {
        private string Name { get; set; }
        private string Endpoint { get; set; }
        private bool Transactional { get; set; }
        private int ThreadCount { get; set; }
        private int NumberOfRetries { get; set; }
        private string LoadBalancerEndpoint { get; set; }
        private string SecurityKey { get; set; }
        protected string LogEndpoint { get; set; }
        private string Path { get; set; }
        private bool EnablePerformanceCounter { get; set; }
        private IsolationLevel QueueIsolationLevel { get; set; }
        private IDictionary<string, HostConfigMessageEndpoint> Messages { get; set; }
        private IList<Assembly> ScanAssemblies { get; set; }

        public HostConfiguration()
        {
            ThreadCount = 1;
            NumberOfRetries = 5;
            Messages = new Dictionary<string, HostConfigMessageEndpoint>();
            ScanAssemblies = new List<Assembly>();
        }

        public HostConfiguration Bus(string endpoint)
        {
            return Bus(endpoint, null);
        }

        public HostConfiguration Bus(string endpoint, string name)
        {
            return Bus(endpoint, name, false);
        }

        public HostConfiguration Bus(string endpoint, string name, bool transactional)
        {
            Endpoint = endpoint;
            Name = name;
            Transactional = transactional;
            return this;
        }

        public HostConfiguration AddAssembly(Assembly assembly)
        {
            ScanAssemblies.Add(assembly);
            return this;
        }

        public HostConfiguration Threads(int threadCount)
        {
            ThreadCount = threadCount;
            return this;
        }

        public HostConfiguration Retries(int numberOfRetries)
        {
            NumberOfRetries = numberOfRetries;
            return this;
        }

        public HostConfiguration LoadBalancer(string endpoint)
        {
            LoadBalancerEndpoint = endpoint;
            return this;
        }

        public HostConfiguration Logging(string endpoint)
        {
            LogEndpoint = endpoint;
            return this;
        }

        public HostConfiguration IsolationLevel(IsolationLevel isolationLevel)
        {
            QueueIsolationLevel = isolationLevel;
            return this;
        }

        public HostConfiguration Security(string key)
        {
            SecurityKey = key;
            return this;
        }

        public HostConfiguration StoragePath(string path)
        {
            Path = path;
            return this;
        }

        public HostConfiguration EnablePerformanceCounters()
        {
            EnablePerformanceCounter = true;
            return this;
        }

        public HostConfiguration Receive(string messageName, string endpoint)
        {
            return Receive(messageName, endpoint, false);
        }

        public HostConfiguration Receive(string messageName, string endpoint, bool transactional)
        {
            Messages.Add(messageName, new HostConfigMessageEndpoint
            {
              Endpoint = endpoint,
              Transactional = transactional,
            });
            return this;
        }

        public virtual BusConfigurationSection ToBusConfiguration()
        {
            var config = new BusConfigurationSection();
            config.Bus.Endpoint = Endpoint;
            config.Bus.ThreadCount = ThreadCount;
            config.Bus.NumberOfRetries = NumberOfRetries;
            config.Bus.Name = Name;
            config.Bus.LoadBalancerEndpoint = LoadBalancerEndpoint;
            config.Bus.LogEndpoint = LogEndpoint;
            config.Bus.QueueIsolationLevel = QueueIsolationLevel.ToString();
            config.Bus.Transactional = Transactional.ToString();
            config.Bus.Path = Path;
            config.Bus.EnablePerformanceCounters = EnablePerformanceCounter;
            config.Security.Key = SecurityKey;
            foreach (var assembly in ScanAssemblies)
                config.Assemblies.Add(new AssemblyElement { Assembly = assembly });
            foreach (var message in Messages)
            {
              config.MessageOwners.Add(new MessageOwnerElement
              {
                Name = message.Key,
                Endpoint = message.Value.Endpoint,
                Transactional = message.Value.Transactional.ToString()
              });
            }
            return config;
        }


        private class HostConfigMessageEndpoint
        {
          public string Endpoint { get; set; }
          public bool Transactional { get; set; }
        }
    }
}