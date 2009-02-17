using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Sagas;
using Xunit;

namespace Rhino.ServiceBus.Tests.Warnings
{
    public class Warnings_would_be_generated
    {
        private readonly WindsorContainer container;

        public Warnings_would_be_generated()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }

        [Fact]
        public void Registering_consumer_with_initiated_by_and_no_saga_should_throw()
        {
            var exception = Assert.Throws<InvalidUsageException>(() => container.AddComponent<ConsumerWithInitiateBy_NoIsaga>());
            Assert.Equal(@"Message consumer: Rhino.ServiceBus.Tests.Warnings.Warnings_would_be_generated+ConsumerWithInitiateBy_NoIsaga implements InitiatedBy<TMsg> but doesn't implment ISaga<TState>. 
Did you forget to inherit from ISaga<TState> ?", exception.Message);
        }

        [Fact]
        public void Registering_consumer_without_initiated_by_and_orchestrate()
        {
            var exception = Assert.Throws<InvalidUsageException>(() => container.AddComponent<ConsumerWithoutInitiateBy_WithOrchestrate>());
            Assert.Equal(@"Message consumer: Rhino.ServiceBus.Tests.Warnings.ConsumerWithoutInitiateBy_WithOrchestrate implements Orchestrates<TMsg> but doesn't implment InitiatedBy<TState>. 
Did you forget to inherit from InitiatedBy<TState> ?", exception.Message);
        }

        public class ConsumerWithInitiateBy_NoIsaga : InitiatedBy<int>
        {
            public void Consume(int message)
            {
                
            }
        }
    }

    internal class ConsumerWithoutInitiateBy_WithOrchestrate : Orchestrates<SagaTests.AddLineItemMessage>
    {
        public void Consume(SagaTests.AddLineItemMessage message)
        {
            
        }
    }
}