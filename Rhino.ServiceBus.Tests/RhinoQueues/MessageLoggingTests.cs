using System;
using System.IO;
using System.Net;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.Mocks;
using Rhino.Queues;
using Rhino.Queues.Model;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Messages;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class MessageLoggingTests : WithDebugging, IDisposable
    {
        private readonly IWindsorContainer container;
        private readonly ITransport transport;
        private readonly IMessageSerializer messageSerializer;
        private readonly QueueManager queue;
        private readonly Uri logEndpoint = new Uri("rhino.queues://localhost:2202/log_endpoint");

        public MessageLoggingTests()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log_endpoint.esent");
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            container = new WindsorContainer();
            new RhinoServiceBusConfiguration()
                .UseCastleWindsor(container)
                .UseStandaloneConfigurationFile("RhinoQueues/RhinoQueues.config")
                .Configure();
            container.Register(Component.For<MessageLoggingModule>());

            messageSerializer = container.Resolve<IMessageSerializer>();
            queue = new QueueManager(new IPEndPoint(IPAddress.Any, 2202), path);
            queue.CreateQueues("log_endpoint");
            queue.Start();
            

            var innerTransport = container.Resolve<ITransport>();
            innerTransport.Start();
            transport = MockRepository.GenerateStub<ITransport>();
            transport.Stub(t => t.Send(null, null)).IgnoreArguments()
                .Do((Delegates.Action<Endpoint, object[]>)(innerTransport.Send));
        }

        [Fact]
        public void Will_send_message_about_serialization_failure()
        {
            var module = container.Resolve<MessageLoggingModule>(new { logQueue = logEndpoint });
            module.Init(transport, null);

            transport.Raise(x => x.MessageSerializationException += null,
                new CurrentMessageInformation { MessageId = Guid.NewGuid() },
                new InvalidOperationException("Operation is not valid due to the current state of the object."));

            var msg = Receive();
            using (var ms = new MemoryStream(msg.Data))
            {
                var serializationError = (SerializationErrorMessage) messageSerializer.Deserialize(ms)[0];
                Assert.Equal("System.InvalidOperationException: Operation is not valid due to the current state of the object.",
                    serializationError.Error);
                Assert.NotEqual(Guid.Empty, serializationError.MessageId);
            }
        }

        [Fact]
        public void Will_send_message_about_message_arrived()
        {
            var module = container.Resolve<MessageLoggingModule>(new { logQueue = logEndpoint});
            module.Init(transport, null);

            transport.Raise(x => x.MessageArrived += null,
                new CurrentMessageInformation
                {
                    MessageId = Guid.NewGuid(),
                    Message = "tst"
                });

            var msg = Receive();

            using (var ms = new MemoryStream(msg.Data))
            {
                var messageArrivedMessage = (MessageArrivedMessage) messageSerializer.Deserialize(ms)[0];
                Assert.NotEqual(Guid.Empty, messageArrivedMessage.MessageId);
                Assert.Equal("tst", messageArrivedMessage.Message);
            }
        }

        [Fact]
        public void Will_send_message_about_message_processing_completed()
        {
            var module = container.Resolve<MessageLoggingModule>(new { logQueue = logEndpoint });
            module.Init(transport, null);

            transport.Raise(x => x.MessageProcessingCompleted += null,
                            new CurrentMessageInformation
                            {
                                MessageId = Guid.NewGuid(),
                                Message = "tst"
                            },
                            new Exception());

            var msg = Receive();

            using (var ms = new MemoryStream(msg.Data))
            {
                var processingCompletedMessage = (MessageProcessingCompletedMessage) messageSerializer.Deserialize(ms)[0];
                Assert.NotEqual(Guid.Empty, processingCompletedMessage.MessageId);
            }
        }

        [Fact]
        public void Will_send_message_about_message_processing_failed()
        {
            var module = container.Resolve<MessageLoggingModule>(new { logQueue = logEndpoint });
            module.Init(transport, null);

            transport.Raise(x => x.MessageProcessingFailure += null,
                new CurrentMessageInformation
                {
                    MessageId = Guid.NewGuid(),
                    Message = "tst"
                },
                new IndexOutOfRangeException("Index was outside the bounds of the array."));

            var msg = Receive();

            using (var ms = new MemoryStream(msg.Data))
            {
                var failedMessage = (MessageProcessingFailedMessage) messageSerializer.Deserialize(ms)[0];
                Assert.NotEqual(Guid.Empty, failedMessage.MessageId);
                Assert.Equal("System.IndexOutOfRangeException: Index was outside the bounds of the array.",
                             failedMessage.ErrorText);
                Assert.Equal("tst", failedMessage.Message);
            }
        }

        [Fact]
        public void Will_send_message_about_message_sent()
        {
            var module = container.Resolve<MessageLoggingModule>(new { logQueue = logEndpoint });
            module.Init(transport, null);

            transport.Raise(x => x.MessageSent += null,
                new CurrentMessageInformation
                {
                    MessageId = Guid.NewGuid(),
                    AllMessages = new[] { "test" }
                });

            var msg = Receive();

            using (var ms = new MemoryStream(msg.Data))
            {
                var failedMessage = (MessageSentMessage) messageSerializer.Deserialize(ms)[0];
                Assert.NotEqual(Guid.Empty, failedMessage.MessageId);
                Assert.Equal(new[] {"test"}, failedMessage.Message);
            }
        }

        [Fact]
        public void Will_send_message_about_message_processing_failed_even_when_rolling_back_tx()
        {
            var module = container.Resolve<MessageLoggingModule>(new { logQueue = logEndpoint });
            module.Init(transport, null);

            using (new TransactionScope())
            {
                transport.Raise(x => x.MessageProcessingFailure += null,
                new CurrentMessageInformation
                {
                    MessageId = Guid.NewGuid(),
                    Message = "tst"
                },
                new IndexOutOfRangeException("Index was outside the bounds of the array."));
            }

            var msg = Receive();

            using (var ms = new MemoryStream(msg.Data))
            {
                var failedMessage = (MessageProcessingFailedMessage) messageSerializer.Deserialize(ms)[0];
                Assert.NotEqual(Guid.Empty, failedMessage.MessageId);
                Assert.Equal("System.IndexOutOfRangeException: Index was outside the bounds of the array.",
                             failedMessage.ErrorText);
                Assert.Equal("tst", failedMessage.Message);
            }
        }

        private Message Receive()
        {
            Message msg;
            using (var tx = new TransactionScope())
            {
                msg = queue.Receive("log_endpoint", TimeSpan.FromSeconds(30));
                tx.Complete();
            }
            return msg;
        }

        public void Dispose()
        {
            queue.Dispose();
            container.Dispose();
        }
    }
}