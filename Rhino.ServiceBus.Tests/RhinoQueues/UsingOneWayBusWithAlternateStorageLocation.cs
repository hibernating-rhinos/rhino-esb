using System;
using System.IO;
using Castle.Windsor;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class UsingOneWayBusWithAlternateStorageLocation : WithDebugging, IDisposable
    {
        private readonly IWindsorContainer container;
        private readonly string defaultStorageLocation;
        private readonly string alternateStorageLocation;
        private readonly string storageDirectory = "one_way.esent";

        public UsingOneWayBusWithAlternateStorageLocation()
        {
            defaultStorageLocation = Directory.GetCurrentDirectory();
            alternateStorageLocation = Path.Combine(Directory.GetCurrentDirectory(), "Alternate");

            if (Directory.Exists(alternateStorageLocation))
                Directory.Delete(alternateStorageLocation, true);

            var defaultOneWayDirectory = Path.Combine(defaultStorageLocation, storageDirectory);
            if (Directory.Exists(defaultOneWayDirectory))
                Directory.Delete(defaultOneWayDirectory, true);

            var hostConfiguration = new HostConfiguration()
                .StoragePath(alternateStorageLocation)
                .Receive("System.string", "rhino.queues://nowhere/no_queue");

            container = new WindsorContainer();
            new OnewayRhinoServiceBusConfiguration()
                .UseConfiguration(hostConfiguration.ToBusConfiguration())
                .UseCastleWindsor(container)
                .Configure();
            container.Resolve<IOnewayBus>();

        }

        [Fact]
        public void Storage_should_be_created_at_alternate_location()
        {
            var expectedDirectory = Path.Combine(alternateStorageLocation, storageDirectory);
            Assert.True(Directory.Exists(expectedDirectory), "Expected directory not found:" + expectedDirectory);
        }

        [Fact]
        public void Storage_should_not_be_created_at_default_location()
        {
            var unexpectedDirectory = Path.Combine(defaultStorageLocation, storageDirectory);
            Assert.False(Directory.Exists(unexpectedDirectory), "Unexpected directory found:" + unexpectedDirectory);
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}