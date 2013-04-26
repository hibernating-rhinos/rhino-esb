using System;
using System.IO;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel;
using Castle.Windsor;
using Rhino.Queues;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.ServiceBus.Serializers;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class WhenErrorOccurs : IDisposable
    {
        private RhinoQueuesTransport transport;
        private readonly ManualResetEvent wait = new ManualResetEvent(false);
        private IMessageSerializer messageSerializer;

        public int FailedCount;

        public WhenErrorOccurs()
        {
            if (Directory.Exists("test.esent"))
                Directory.Delete("test.esent", true);
        }

        [Fact]
        public void Deserialization_Error_Will_Not_Retry()
        {
            var serviceLocator = new CastleServiceLocator(new WindsorContainer());
            messageSerializer = new ThrowingSerializer(new XmlMessageSerializer(new DefaultReflection(),
                                                      serviceLocator));
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
            var count = 0;
            transport.MessageProcessingFailure += (messageInfo, ex) =>
            {
                count++;
            };
            transport.Send(transport.Endpoint, new object[] { "test" });

            wait.WaitOne(TimeSpan.FromSeconds(5));

            Assert.Equal(1, count);
        }

        [Fact]
        public void Arrived_Error_Will_Retry_Number_Of_Times_Configured()
        {
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
            var count = 0;
            transport.MessageArrived += info =>
            {
                throw new InvalidOperationException();
            };
            transport.MessageProcessingFailure += (messageInfo, ex) =>
            {
                count++;
            };
            transport.Send(transport.Endpoint, new object[] { "test" });

            wait.WaitOne(TimeSpan.FromSeconds(5));

            Assert.Equal(5, count);
        }

        public void Dispose()
        {
            transport.Dispose();
        }
    }

    public class ThrowingSerializer : IMessageSerializer
    {
        private readonly XmlMessageSerializer serializer;

        public ThrowingSerializer(XmlMessageSerializer serializer)
        {
            this.serializer = serializer;
        }

        public void Serialize(object[] messages, Stream message)
        {
            serializer.Serialize(messages, message);
        }

        public object[] Deserialize(Stream message)
        {
            throw new NotImplementedException();
        }
    }


}