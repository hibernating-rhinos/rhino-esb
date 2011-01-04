using System.Collections.Generic;
using Rhino.ServiceBus.Config;

namespace Rhino.ServiceBus.Hosting
{
    public class HostConfiguration
    {
        private string Name { get; set; }
        private string Endpoint { get; set; }
        private int ThreadCount { get; set; }
        private int NumberOfRetries { get; set; }
        private string LoadBalancerEndpoint { get; set; }
        protected string LogEndpoint { get; set; }
        private IDictionary<string, string> Messages { get; set; }

        public HostConfiguration()
        {
            ThreadCount = 1;
            NumberOfRetries = 5;
            Messages = new Dictionary<string, string>();
        }

        public HostConfiguration Bus(string endpoint)
        {
            Endpoint = endpoint;
            return this;
        }

        public HostConfiguration Bus(string endpoint, string name)
        {
            Bus(endpoint);
            Name = name;
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

        public HostConfiguration Receive(string messageName, string endpoint)
        {
            Messages.Add(messageName, endpoint);
            return this;
        }

        public BusConfigurationSection ToBusConfiguration()
        {
            var config = new BusConfigurationSection();
            config.Bus.Endpoint = Endpoint;
            config.Bus.ThreadCount = ThreadCount;
            config.Bus.NumberOfRetries = NumberOfRetries;
            config.Bus.Name = Name;
            config.Bus.LoadBalancerEndpoint = LoadBalancerEndpoint;
            config.Bus.LogEndpoint = LogEndpoint;
            foreach (var message in Messages)
            {
                config.MessageOwners.Add(new MessageOwnerElement{Name = message.Key, Endpoint = message.Value});
            }
            return config;
        }
    }
}