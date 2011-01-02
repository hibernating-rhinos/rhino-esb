using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class MessageModuleTests : MsmqTestBase
    {
        private IWindsorContainer container;

        public MessageModuleTests()
        {
            Module2.Stopped = Module2.Started = false;

            container = new WindsorContainer(new XmlInterpreter());
            new RhinoServiceBusFacility()
                .UseCastleWindsor(container)
                .AddMessageModule<Module1>()
                .AddMessageModule<Module2>()
                .Configure();

            container.Register(Component.For<BadHandler>());
        }

        [Fact]
        public void Can_specify_modules_to_register_in_the_service_bus()
        {
            var serviceBus = (DefaultServiceBus)container.Resolve<IServiceBus>();
            Assert.Equal(2, serviceBus.Modules.Length);
        }

        [Fact]
        public void Can_specify_modules_in_order_to_be_registered_in_the_service_bus()
        {
            var serviceBus = (DefaultServiceBus)container.Resolve<IServiceBus>();
            Assert.IsType<Module1>(serviceBus.Modules[0]);
            Assert.IsType<Module2>(serviceBus.Modules[1]);
        }

		[Fact]
		public void Disabling_queue_init_module()
		{
			Module2.Stopped = Module2.Started = false;

			container = new WindsorContainer(new XmlInterpreter());
			new RhinoServiceBusFacility()
				.AddMessageModule<Module1>()
				.AddMessageModule<Module2>()
				.DisableQueueAutoCreation()
                .UseCastleWindsor(container)
                .Configure();


			var serviceBus = (DefaultServiceBus)container.Resolve<IServiceBus>();
			Assert.IsType<Module1>(serviceBus.Modules[0]);
			Assert.IsType<Module2>(serviceBus.Modules[1]);
		}

        [Fact]
        public void When_bus_is_started_modules_will_be_initalized()
        {
            using(var serviceBus = (DefaultServiceBus)container.Resolve<IServiceBus>())
            {
                serviceBus.Start();

                Assert.True(Module2.Started);
            }
        }

        [Fact]
        public void When_bus_is_stopped_modules_will_be_stopped()
        {
            using (var serviceBus = (DefaultServiceBus)container.Resolve<IServiceBus>())
            {
                serviceBus.Start();

            }
            Assert.True(Module2.Stopped);
        }

        [Fact]
        public void Can_register_to_get_message_failure_notification()
        {
            using (var serviceBus = (DefaultServiceBus)container.Resolve<IServiceBus>())
            {
                serviceBus.Start();
                Module1.ErrorResetEvent = new ManualResetEvent(false);

                serviceBus.Send(serviceBus.Endpoint,5);
                Module1.ErrorResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);
            }
            Assert.NotNull(Module1.Exception);
        }

        [Fact]
        public void Can_register_to_get_message_completion_notification_even_on_error()
        {
        	using (var serviceBus = (DefaultServiceBus) container.Resolve<IServiceBus>())
        	{
        		serviceBus.Start();
        		Module1.CompletionResetEvent = new ManualResetEvent(false);
        		Module1.ErrorResetEvent = new ManualResetEvent(false);

        		Module1.Completion = false;

        		serviceBus.Send(serviceBus.Endpoint, 3);
        		Module1.ErrorResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);
        		Module1.CompletionResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);
        	}
        	Assert.True(Module1.Completion);
        }

    	[Fact]
        public void Can_register_to_get_message_completion_notification()
    	{
    		using (var serviceBus = (DefaultServiceBus) container.Resolve<IServiceBus>())
    		{
    			serviceBus.Start();
    			Module1.CompletionResetEvent = new ManualResetEvent(false);
    			Module1.Completion = false;

    			serviceBus.Send(serviceBus.Endpoint, "hello");
    			Module1.CompletionResetEvent.WaitOne(TimeSpan.FromSeconds(30), false);
    		}
    		Assert.True(Module1.Completion);
    	}

    	public class Module1 : IMessageModule
        {
            public static Exception Exception;
            public static ManualResetEvent ErrorResetEvent;
            public static ManualResetEvent CompletionResetEvent;
            public static bool Completion = true;

			public void Init(ITransport transport, IServiceBus bus)
            {
                transport.MessageProcessingFailure+=Transport_OnMessageProcessingFailure;
                transport.MessageProcessingCompleted+=Transport_OnMessageProcessingCompleted;
            }

            private static void Transport_OnMessageProcessingCompleted(CurrentMessageInformation t, Exception e)
            {
            	Completion = true;
            	CompletionResetEvent.Set();
            }

    		private static void Transport_OnMessageProcessingFailure(CurrentMessageInformation t1, Exception t2)
            {
                Exception = t2;
                ErrorResetEvent.Set();
            }

			public void Stop(ITransport transport, IServiceBus bus)
            {
            }
        }

        public class Module2 : IMessageModule
        {
            public static bool Started, Stopped;

			public void Init(ITransport transport, IServiceBus bus)
            {
                Started = true;
            }

			public void Stop(ITransport transport, IServiceBus bus)
            {
                Stopped = true;
            }
        }

        public class BadHandler : ConsumerOf<int>
        {
            public void Consume(int message)
            {
                throw new System.NotImplementedException();
            }
        }

        public class SimpleHandler : ConsumerOf<string>
        {
            public void Consume(string message)
            {
                
            }
        }
    }
}