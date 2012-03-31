using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Impl;
using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.Castle
{
    public class ContainerTests
    {
        private IWindsorContainer container;
        private IServiceBus bus;

        public ContainerTests()
        {
            this.container = new WindsorContainer();

            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .Configure();

            this.bus = container.Resolve<IServiceBus>();
        }

        [Fact]
        public void Disposable_Consumer_is_disposed()
        {
            this.container.Register(Component.For<DisposableConsumer>().LifeStyle.Transient);

            DisposableConsumer.ResetCounters();
            Assert.Equal(0, DisposableConsumer.ConsumedMessages);
            Assert.Equal(0, DisposableConsumer.NotDisposedInstances);

            bus.ConsumeMessages("TestMessage");

            Assert.Equal(1, DisposableConsumer.ConsumedMessages);
            Assert.Equal(0, DisposableConsumer.NotDisposedInstances);
        }

        [Fact]
        public void Disposable_dependency_of_a_simple_Consumer_is_disposed()
        {
            this.container.Register(Component.For<DisposableDependency>().LifeStyle.Transient);
            this.container.Register(Component.For<ConsumerWithDisposableDependency>().LifeStyle.Transient);

            ConsumerWithDisposableDependency.ResetCounter();
            DisposableDependency.ResetCounter();
            Assert.Equal(0, ConsumerWithDisposableDependency.ConsumedMessages);
            Assert.Equal(0, DisposableDependency.NotDisposedInstances);

            this.bus.ConsumeMessages("TestMessage");

            Assert.Equal(1, ConsumerWithDisposableDependency.ConsumedMessages);
            Assert.Equal(0, DisposableDependency.NotDisposedInstances);
        }
    }

    public class DisposableConsumer : ConsumerOf<string>, IDisposable
    {
        public static long NotDisposedInstances = 0;
        public static long ConsumedMessages = 0;

        private bool disposed;

        public DisposableConsumer()
        {
            NotDisposedInstances += 1;
        }

        public void Consume(string message)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            Assert.True(1 <= NotDisposedInstances);

            ConsumedMessages += 1;
        }

        public void Dispose()
        {
            if (this.disposed == false)
            {
                NotDisposedInstances -= 1;
                this.disposed = true;
            }
        }

        public static void ResetCounters()
        {
            NotDisposedInstances = 0;
            ConsumedMessages = 0;
        }
    }

    public class DisposableDependency : IDisposable
    {
        public static long NotDisposedInstances = 0;

        private bool disposed;

        public DisposableDependency()
        {
            NotDisposedInstances += 1;
        }

        public void Use()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            Assert.True(1 <= NotDisposedInstances);
        }

        public void Dispose()
        {
            if (this.disposed == false)
            {
                NotDisposedInstances -= 1;
                this.disposed = true;
            }
        }

        public static void ResetCounter()
        {
            NotDisposedInstances = 0;
        }
    }

    public class ConsumerWithDisposableDependency : ConsumerOf<string>
    {
        public static long ConsumedMessages = 0;

        private DisposableDependency disposableDependency;

        public ConsumerWithDisposableDependency(DisposableDependency disposableDependency)
        {
            this.disposableDependency = disposableDependency;

            ConsumedMessages += 1;
        }

        public void Consume(string message)
        {
            this.disposableDependency.Use();
        }

        public static void ResetCounter()
        {
            ConsumedMessages = 0;
        }
    }
}
