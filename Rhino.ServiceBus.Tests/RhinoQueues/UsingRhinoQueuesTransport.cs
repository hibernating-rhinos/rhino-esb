using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel;
using Castle.Windsor;
using Rhino.Queues;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.ServiceBus.Serializers;
using Rhino.ServiceBus.Transport;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class UsingRhinoQueuesTransport : WithDebugging, IDisposable
    {
        private readonly RhinoQueuesTransport transport;
        private readonly ManualResetEvent wait = new ManualResetEvent(false);
        private readonly XmlMessageSerializer messageSerializer;

        public UsingRhinoQueuesTransport()
        {
            if (Directory.Exists("test.esent"))
                Directory.Delete("test.esent", true);

            var serviceLocator = new CastleServiceLocator(new WindsorContainer());
            messageSerializer = new XmlMessageSerializer(new DefaultReflection(), serviceLocator);
            transport = new RhinoQueuesTransport(
                new Uri("rhino.queues://localhost:23456/q"),
                new EndpointRouter(),
                messageSerializer,
                1,
                "test.esent",
                IsolationLevel.Serializable, 
                5,
                false,
                new RhinoQueuesMessageBuilder(messageSerializer, serviceLocator),
                new QueueManagerConfiguration()
                );
            transport.Start();
        }

        [Fact]
        public void Can_send_and_receive_message()
        {
            string val = null;
            transport.MessageArrived += information =>
            {
                val = (string)information.Message;
                wait.Set();
                return true;
            };
            
            using(var tx = new TransactionScope())
            {
                transport.Send(transport.Endpoint, new object[] { "test" });
                tx.Complete();
            }

            wait.WaitOne(TimeSpan.FromSeconds(5), false);
            Assert.Equal("test", val);
        }

        [Fact]
        public void Will_retry_message_if_error_happened()
        {
            string val = null;
            bool first = true;
            transport.MessageArrived += information =>
            {
                if(first)
                {
                    first = false;
                    throw new InvalidOperationException("problem" );
                }
                val = (string)information.Message;
                wait.Set();
                return true;
            };

            using (var tx = new TransactionScope())
            {
                transport.Send(transport.Endpoint, new object[] { "test" });
                tx.Complete();
            }

            wait.WaitOne(TimeSpan.FromSeconds(5), false);
            Assert.Equal("test", val);
        }

        [Fact]
        public void Will_move_poision_message_to_error_queue()
        {
            transport.MessageArrived += information =>
            {
                throw new InvalidOperationException("problem" );
            };

            using (var tx = new TransactionScope())
            {
                transport.Send(transport.Endpoint, new object[] { "test" });
                tx.Complete();
            }

            using(var tx = new TransactionScope())
            {
                var message = transport.Queue.Receive(SubQueue.Errors.ToString());

                Assert.Equal("test", messageSerializer.Deserialize(new MemoryStream(message.Data))[0]);
                var errMsg = transport.Queue.Receive(SubQueue.Errors.ToString());
                Assert.True(Encoding.Unicode.GetString(errMsg.Data).StartsWith("System.InvalidOperationException: problem"));

                Assert.Equal(message.Id.ToString(), errMsg.Headers["correlation-id"]);
                Assert.Equal("5", errMsg.Headers["retries"]);

                tx.Complete();
            }
        }

        [Fact]
        public void Can_send_timed_messages()
        {
            DateTime val = DateTime.MinValue;
            transport.MessageArrived += information =>
            {
                val = DateTime.Now;
                wait.Set();
                return true;
            };
            DateTime sendTime;
            using (var tx = new TransactionScope())
            {
                sendTime = DateTime.Now;
                transport.Send(transport.Endpoint, sendTime.AddSeconds(3), new object[] { "test" });
                tx.Complete();
            }

            Assert.True(wait.WaitOne(TimeSpan.FromSeconds(10), false));

            Assert.InRange(val - sendTime, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Can_send_multiple_timed_messages_for_the_same_time()
        {
            int receivedCount = 0;
            transport.MessageArrived += information =>
            {
                receivedCount++;
                if (receivedCount == 2)
                    wait.Set();
                return true;
            };

            DateTime sendTime = DateTime.Now.AddSeconds(2);
            using (var tx = new TransactionScope())
            {
                transport.Send(transport.Endpoint, sendTime, new object[] { "test1" });
                transport.Send(transport.Endpoint, sendTime, new object[] { "test2" });
                tx.Complete();
            }

            Assert.True(wait.WaitOne(TimeSpan.FromSeconds(10), false));
        }

        public void Dispose()
        {
            transport.Dispose();
        }
    }
}