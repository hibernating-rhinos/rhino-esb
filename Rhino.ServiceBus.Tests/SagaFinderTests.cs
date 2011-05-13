using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Sagas;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class SagaFinderTests : MsmqTestBase
    {
        private static readonly ManualResetEvent wait = new ManualResetEvent(false);
        private readonly IWindsorContainer container;

        public SagaFinderTests()
        {
            container = new WindsorContainer(new XmlInterpreter());
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .Configure();
            container.Register(
                Component.For<TestSagaFinder>(),
                Component.For<SagaFinderTestSaga>()
                );
        }

        [Fact]
        public void Can_implement_saga_finder_multiple_times()
        {
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                bus.Send(bus.Endpoint, new TestMessage());
                bool signaled = wait.WaitOne(TimeSpan.FromSeconds(5));
                Assert.True(signaled);

                wait.Reset();
                bus.Send(bus.Endpoint, new TestMessage2());
                signaled = wait.WaitOne(TimeSpan.FromSeconds(5));
                Assert.True(signaled);
            }
        }

        public class SagaFinderTestSaga : ISaga<object>,
                                          InitiatedBy<TestMessage>,
                                          Orchestrates<TestMessage2>
        {
            public static readonly SagaFinderTestSaga Instance = new SagaFinderTestSaga();
            public Guid Id { get; set; }
            public bool IsCompleted { get; set; }
            public object State { get; set; }

            public void Consume(TestMessage message)
            {
                wait.Set();
            }

            public void Consume(TestMessage2 message)
            {
                wait.Set();
            }
        }

        public class TestMessage
        {
        }

        public class TestMessage2
        {
        }

        public class TestSagaFinder : FinderOf<SagaFinderTestSaga>.By<TestMessage>,
                                      FinderOf<SagaFinderTestSaga>.By<TestMessage2>
        {
            public SagaFinderTestSaga FindBy(TestMessage message)
            {
                return SagaFinderTestSaga.Instance;
            }

            public SagaFinderTestSaga FindBy(TestMessage2 message)
            {
                return SagaFinderTestSaga.Instance;
            }
        }
    }
}