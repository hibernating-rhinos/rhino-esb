using System;
using System.Messaging;
using System.Net;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class When_using_ip_of_machine : MsmqTestBase
    {
        [Fact]
        public void Can_start_bus()
        {
            var address = Dns.GetHostAddresses(Environment.MachineName)
                .First(x => x.ToString().Contains(":") == false);
            var endpoint = new Uri("msmq://" + address + "/test_queue");
            var flatQueueStrategy = new FlatQueueStrategy(new EndpointRouter(),
                                                          endpoint);
            flatQueueStrategy.InitializeQueue(new Endpoint
            {
                Uri = endpoint
            }, QueueType.Standard);
        }
    }
}