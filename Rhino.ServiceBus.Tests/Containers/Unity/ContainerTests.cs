
using System;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Unity;
using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.Unity
{
    public class ContainerTests
    {
        [Fact]
        public void Consumer_must_be_transient()
        {
            var container = new UnityContainer();
            container.RegisterType<TestConsumer>(new ContainerControlledLifetimeManager());
            new RhinoServiceBusConfiguration()
                .UseUnity(container)
                .Configure();

            try
            {
                container.Resolve<TestConsumer>();
            }
            catch (Exception ex)
            {
                Assert.True(null != ex.InnerException as InvalidUsageException);
            }
        }
    }
}