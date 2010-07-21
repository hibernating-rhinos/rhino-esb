using System;
using System.IO;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.ServiceBus.Serializers;
using Xunit;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
	public class WhenSerializationErrorOccurs : IDisposable
	{
        private readonly RhinoQueuesTransport transport;
        private readonly ManualResetEvent wait = new ManualResetEvent(false);
        private readonly IMessageSerializer messageSerializer;
		
		public int FailedCount;

        public WhenSerializationErrorOccurs()
        {
            if (Directory.Exists("test.esent"))
                Directory.Delete("test.esent", true);

            messageSerializer = new ThrowingSerializer(new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel()));
            transport = new RhinoQueuesTransport(
                new Uri("rhino.queues://localhost:23456/q"),
                new EndpointRouter(),
                messageSerializer,
                1,
                "test.esent",
                IsolationLevel.Serializable, 
                5,
                new RhinoQueuesMessageBuilder(messageSerializer)
                );
            transport.Start();
        }
		
		[Fact]
		public void Will_Retry_Number_Of_Times_Configured()
		{
			var count = 0;
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