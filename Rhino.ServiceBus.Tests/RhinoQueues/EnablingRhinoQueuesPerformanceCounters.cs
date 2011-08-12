using System;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Castle.Windsor;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Xunit;
using Rhino.Queues.Monitoring;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class EnablingRhinoQueuesPerformanceCounters : WithDebugging, IDisposable
    {
        private IWindsorContainer container;
        private IStartableServiceBus bus;

        [Fact]
        public void Enabling_performance_counters_should_result_in_performance_counters_being_created()
        {
            //This will delete an recreate the categories.
            PerformanceCategoryCreator.CreateCategories();

            var outboundIntances = new PerformanceCounterCategory(OutboundPerfomanceCounters.CATEGORY).GetInstanceNames();
            var inboundIntances = new PerformanceCounterCategory(InboundPerfomanceCounters.CATEGORY).GetInstanceNames();
            Assert.Empty(outboundIntances);
            Assert.Empty(inboundIntances);

            var hostConfiguration = new HostConfiguration()
                .EnablePerformanceCounters()
                .Bus("rhino.queues://localhost/test_queue2", "test");

            container = new WindsorContainer();
            new RhinoServiceBusConfiguration()
                .UseConfiguration(hostConfiguration.ToBusConfiguration())
                .UseCastleWindsor(container)
                .Configure();
            bus = container.Resolve<IStartableServiceBus>();
            bus.Start();

            using (var tx = new TransactionScope())
            {
                bus.Send(bus.Endpoint, "test message.");
                tx.Complete();
            }

            outboundIntances = new PerformanceCounterCategory(OutboundPerfomanceCounters.CATEGORY).GetInstanceNames();
            inboundIntances = new PerformanceCounterCategory(InboundPerfomanceCounters.CATEGORY).GetInstanceNames();

            Assert.NotEmpty(outboundIntances.Where(name => name.Contains("test_queue2")));
            Assert.NotEmpty(inboundIntances.Where(name => name.Contains("test_queue2")));

        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}