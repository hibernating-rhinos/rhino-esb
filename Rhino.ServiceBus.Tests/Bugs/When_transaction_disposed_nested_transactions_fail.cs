using System;
using System.Linq;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    // ReSharper disable InconsistentNaming
    public class When_transaction_disposed_nested_transactions_fail
    {
        private MemoryAppender memoryAppender;

        public When_transaction_disposed_nested_transactions_fail()
        {
            log4net.Config.BasicConfigurator.Configure();
            memoryAppender = new MemoryAppender();
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.AddAppender(memoryAppender);
        }

        private static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer(new XmlInterpreter());
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .AddMessageModule<TestMessageModule>()
                .Configure();
            return container;
        }

        [Fact]
        public void Should_allow_nested_transaction_to_be_disposed()
        {
            var container = CreateContainer();
            container.Register(Component.For<TestConsumer>());

            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();
                bus.SendToSelf(new TestMessage());
            }

            AssertThatNestedTransactionExceptionHasNotBeenThrown();
        }

        private void AssertThatNestedTransactionExceptionHasNotBeenThrown()
        {
            var transactionExceptions = memoryAppender.GetEvents().Where(entry =>
                    entry.ExceptionObject is InvalidOperationException &&
                    entry.ExceptionObject.Message.ToLower().Contains("transactionscope nested incorrectly"));

            Assert.True(transactionExceptions.Count() == 0, "'TransactionScope nested incorrectly' exception was thrown");
        }

        public class TestMessage
        {
        }

        public class TestConsumer : ConsumerOf<TestMessage>
        {
            public void Consume(TestMessage message)
            {
                throw new Exception("ERROR");
            }
        }

        public class TestMessageModule : IMessageModule
        {
            [ThreadStatic]
            private static TransactionScope NestedTransactionScope;

            public void Init(ITransport transport, IServiceBus bus)
            {
                transport.MessageArrived += MessageArrived;
                transport.MessageProcessingCompleted += MessageProcessingCompleted;
                transport.MessageProcessingFailure += MessageProcessingFailed;
                transport.BeforeMessageTransactionCommit += BeforeCommit;
            }
            
            public void Stop(ITransport transport, IServiceBus bus)
            {
                transport.MessageArrived -= MessageArrived;
                transport.MessageProcessingCompleted -= MessageProcessingCompleted;
                transport.MessageProcessingFailure -= MessageProcessingFailed;
                transport.BeforeMessageTransactionCommit -= BeforeCommit;
            }

            private bool MessageArrived(CurrentMessageInformation arg)
            {
                if (NestedTransactionScope == null)
                {
                    NestedTransactionScope = new TransactionScope(TransactionScopeOption.Suppress);
                }

                return false;
            }

            private void MessageProcessingCompleted(CurrentMessageInformation arg1, Exception arg2)
            {
                DisposeNestedTransaction();
            }

            private void MessageProcessingFailed(CurrentMessageInformation arg1, Exception arg2)
            {
               DisposeNestedTransaction();
            }

            private void BeforeCommit(CurrentMessageInformation obj)
            {
                DisposeNestedTransaction();
            }

            private void DisposeNestedTransaction()
            {
                if (NestedTransactionScope == null)
                    return;

                NestedTransactionScope.Dispose();
                NestedTransactionScope = null;
            }
        }
    }
    // ReSharper restore InconsistentNaming
}