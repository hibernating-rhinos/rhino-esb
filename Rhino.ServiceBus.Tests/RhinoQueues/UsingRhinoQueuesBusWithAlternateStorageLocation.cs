using System;
using System.IO;
using System.Linq;
using Castle.Windsor;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class UsingRhinoQueuesBusWithAlternateStorageLocation : WithDebugging, IDisposable
    {
        private readonly IWindsorContainer container;
        private readonly IStartableServiceBus bus;
        private readonly string defaultStorageLocation;
        private readonly string alternateStorageLocation;
        private readonly string[] storageDirectories;

        public UsingRhinoQueuesBusWithAlternateStorageLocation()
        {
            defaultStorageLocation = Directory.GetCurrentDirectory();
            alternateStorageLocation = Path.Combine(Directory.GetCurrentDirectory(), "Alternate");

            storageDirectories = new[] { "test.esent", "test_subscriptions.esent" };

            if (Directory.Exists(alternateStorageLocation))
                Directory.Delete(alternateStorageLocation, true);

            foreach (var expectedSubDirectory in storageDirectories.Select(d => Path.Combine(defaultStorageLocation, d)))
            {
                if (Directory.Exists(expectedSubDirectory))
                    Directory.Delete(expectedSubDirectory, true);
            }

            var hostConfiguration = new HostConfiguration()
                .StoragePath(alternateStorageLocation)
                .Bus("rhino.queues://localhost/test_queue2", "test")
                .Receive("Rhino.ServiceBus.Tests", "rhino.queues://localhost/test_queue");

            container = new WindsorContainer();
            new RhinoServiceBusConfiguration()
                .UseConfiguration(hostConfiguration.ToBusConfiguration())
                .UseCastleWindsor(container)
                .Configure();
            bus = container.Resolve<IStartableServiceBus>();
            bus.Start();
        }

        [Fact]
        public void Storage_should_be_created_at_alternate_location()
        {
            foreach (var expectedDirectory in
                storageDirectories.Select(expectedSubDirectory => 
                                           Path.Combine(alternateStorageLocation, expectedSubDirectory)))
            {
                Assert.True(Directory.Exists(expectedDirectory), "Expected directory not found:" + expectedDirectory);
            }
        }

        [Fact]
        public void Storage_should_not_be_created_at_default_location()
        {
            foreach (var unexpectedDirectory in
                storageDirectories.Select(expectedSubDirectory => 
                                           Path.Combine(defaultStorageLocation, expectedSubDirectory)))
            {
                Assert.False(Directory.Exists(unexpectedDirectory), "Unexpected directory found:" + unexpectedDirectory);
            }
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}