using System;
using System.IO;
using Castle.Windsor;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class UsingOneWayBusWithBusNameSpecified : WithDebugging, IDisposable
    {
        private const string DEFAULT_STORAGE_DIRECTORY = "one_way.esent";
        private const string ALTERNATE_BUS_NAME = "another_one_way_bus";
        private const string ALTERNATE_STORAGE_DIRECTORY = ALTERNATE_BUS_NAME + ".esent";

        private readonly IWindsorContainer container;
        private readonly string baseStorageLocation;
        private readonly string defaultOneWayDirectory;
        private readonly string alternateOneWayDirectory;

        public UsingOneWayBusWithBusNameSpecified()
        {
            baseStorageLocation = Directory.GetCurrentDirectory();

            defaultOneWayDirectory = Path.Combine(baseStorageLocation, DEFAULT_STORAGE_DIRECTORY);
            if (Directory.Exists(defaultOneWayDirectory))
                Directory.Delete(defaultOneWayDirectory, true);

            alternateOneWayDirectory = Path.Combine(baseStorageLocation, ALTERNATE_STORAGE_DIRECTORY);
            if (Directory.Exists(alternateOneWayDirectory))
                Directory.Delete(alternateOneWayDirectory, true);

            var hostConfiguration = new HostConfiguration()
                .Bus(null, ALTERNATE_BUS_NAME)
                .Receive("System.string", "rhino.queues://nowhere/no_queue");

            container = new WindsorContainer();
            new OnewayRhinoServiceBusConfiguration()
                .UseConfiguration(hostConfiguration.ToBusConfiguration())
                .UseCastleWindsor(container)
                .Configure();
            container.Resolve<IOnewayBus>();

        }

        [Fact]
        public void Storage_should_be_created_base_on_bus_name_location()
        {
            Assert.True(Directory.Exists(alternateOneWayDirectory), "Expected directory not found:" + alternateOneWayDirectory);
        }

        [Fact]
        public void Storage_should_not_be_created_at_default_location()
        {
            Assert.False(Directory.Exists(defaultOneWayDirectory), "Unexpected directory found:" + defaultOneWayDirectory);
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}