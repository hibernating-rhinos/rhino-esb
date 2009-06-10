using System.Collections.Generic;
using Castle.Core.Configuration;

namespace Rhino.ServiceBus.Hosting
{
    public class HostConfiguration
    {
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

        public IConfiguration ToIConfiguration()
        {
            var config = new MutableConfiguration("rhino.esb");

            var busConfig = config.CreateChild("bus")
                .Attribute("endpoint", Endpoint)
                .Attribute("threadCount", ThreadCount.ToString())
                .Attribute("numberOfRetries", NumberOfRetries.ToString());

            if (string.IsNullOrEmpty(LoadBalancerEndpoint) == false)
                busConfig.Attribute("loadBalancerEndpoint", LoadBalancerEndpoint);

            if (string.IsNullOrEmpty(LogEndpoint) == false)
                busConfig.Attribute("logEndpoint", LogEndpoint);

            var messagesConfig = config.CreateChild("messages");

            foreach (var message in Messages)
            {
                messagesConfig.CreateChild("add")
                    .Attribute("name", message.Key)
                    .Attribute("endpoint", message.Value);
            }

            return config;
        }
    }
}