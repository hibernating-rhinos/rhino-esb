using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Config;
using System.IO;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class ConfigReaderTests
    {

        [Fact]
        public void Can_get_path_variable()
        {
            var bus = new BusElement();
            var expected = "folder/app/one_way";
            bus.Path = "folder/app";

            Assert.Equal(expected+".esent", bus.QueuePath);
            Assert.Equal(expected + "_subscriptions.esent", bus.SubscriptionPath);
        }

        [Fact]
        public void Can_get_path_variable_replacement()
        {
            var bus = new BusElement();
            var expected = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "app"), "one_way");
            bus.Path = "%APPDATA%/app";

            Assert.Equal(expected+".esent", bus.QueuePath);
            Assert.Equal(expected + "_subscriptions.esent", bus.SubscriptionPath);
        }
    }
}