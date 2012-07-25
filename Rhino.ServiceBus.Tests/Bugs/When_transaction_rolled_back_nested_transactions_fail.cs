using System;
using System.Linq;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;

using log4net.Appender;
using log4net.Repository.Hierarchy;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
  // ReSharper disable InconsistentNaming
  public class When_transaction_rolled_back_nested_transactions_fail
  {
    private readonly MemoryAppender memoryAppender;
    public static ManualResetEvent wait;

    public When_transaction_rolled_back_nested_transactions_fail()
    {
      log4net.Config.BasicConfigurator.Configure();
      memoryAppender = new MemoryAppender();
      var hierarchy = (Hierarchy)LogManager.GetRepository();
      hierarchy.Root.RemoveAllAppenders();
      hierarchy.Root.AddAppender(memoryAppender);
    }

    private static IWindsorContainer SetupBusForMsmq()
    {
      var container = new WindsorContainer(new XmlInterpreter());
      new RhinoServiceBusConfiguration()
          .UseCastleWindsor(container)
          .UseStandaloneConfigurationFile("BusOnTransactionalQueue.config")
          .AddMessageModule<TestMessageModule>()
          .Configure();
      return container;
    }

    private static IWindsorContainer SetupBusForRhinoQueues()
    {
      var container = new WindsorContainer(new XmlInterpreter());
      new RhinoServiceBusConfiguration()
          .UseCastleWindsor(container)
          .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
          .AddMessageModule<TestMessageModule>()
          .Configure();
      return container;
    }

    [Fact]
    public void Should_allow_nested_transaction_to_be_rolled_back_using_msmq()
    {
      var container = SetupBusForMsmq();
      container.Register(Component.For<TestConsumer>());

      bool signalled;
      using (var bus = container.Resolve<IStartableServiceBus>())
      {
        wait = new ManualResetEvent(false);
        bus.Start();
        bus.SendToSelf(new TestMessage());
        signalled = wait.WaitOne(TimeSpan.FromSeconds(5), false);
      }
      
      Assert.True(signalled);
      AssertThatNestedTransactionExceptionHasNotBeenThrown();
    }

    [Fact]
    public void Should_allow_nested_transaction_to_be_rolled_back_using_rhino_queues()
    {
      var container = SetupBusForRhinoQueues();
      container.Register(Component.For<TestConsumer>());

      bool signalled;
      using (var bus = container.Resolve<IStartableServiceBus>())
      {
        wait = new ManualResetEvent(false);
        bus.Start();
        bus.SendToSelf(new TestMessage());
        signalled = wait.WaitOne(TimeSpan.FromSeconds(5), false);
      }

      Assert.True(signalled);
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
        transport.BeforeMessageTransactionRollback += BeforeMessageTransactionRollback;
      }

      public void Stop(ITransport transport, IServiceBus bus)
      {
        transport.MessageArrived -= MessageArrived;
        transport.BeforeMessageTransactionRollback -= BeforeMessageTransactionRollback;
      }

      private bool MessageArrived(CurrentMessageInformation arg)
      {
        if (NestedTransactionScope == null)
        {
          NestedTransactionScope = new TransactionScope(TransactionScopeOption.Suppress);
        }

        return false;
      }

      private void BeforeMessageTransactionRollback(CurrentMessageInformation obj)
      {
        DisposeNestedTransaction();
        wait.Set();
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