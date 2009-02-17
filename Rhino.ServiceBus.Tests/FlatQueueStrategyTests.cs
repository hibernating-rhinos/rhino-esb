using System;
using System.Messaging;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class FlatQueueStrategyTests : MsmqFlatQueueTestBase
    {
    	private readonly OpenedQueue openedQueue;

    	public FlatQueueStrategyTests()
    	{
    		openedQueue = MsmqUtil.GetQueuePath(testQueueEndPoint).Open();
    	}

		public override void Dispose()
		{
			base.Dispose();
			openedQueue.Dispose();
		}

        [Fact]
        public void Moving_to_errors_queue_removes_message_from_subscriptions_queue()
        {
            var queueStrategy = new FlatQueueStrategy(new EndpointRouter(),testQueueEndPoint.Uri);
			openedQueue.Send(new Message(new TestMessage { Name = "ayende" }));
            Message msg = openedQueue.Peek(TimeSpan.FromSeconds(30));
            Assert.Equal(1, openedQueue.GetMessageCount());
            string msgId;
            queueStrategy.TryMoveMessage(openedQueue, msg, SubQueue.Errors,out msgId);
            Assert.Equal(0, openedQueue.GetMessageCount());
        }

        [Fact]
        public void Moving_to_discarded_queue_removes_message_from_subscriptions_queue()
        {
            var queueStrategy = new FlatQueueStrategy(new EndpointRouter(), testQueueEndPoint.Uri);
			openedQueue.Send(new Message(new TestMessage { Name = "ayende" }));
            Message msg = openedQueue.Peek(TimeSpan.FromSeconds(30));
            Assert.Equal(1, openedQueue.GetMessageCount());
            string msgId;
            queueStrategy.TryMoveMessage(openedQueue, msg, SubQueue.Discarded, out msgId);
            Assert.Equal(0, openedQueue.GetMessageCount());
        }

        [Fact]
        public void Moving_to_subscription_queue_removes_message_from_root_queue()
        {
            var queueStrategy = new FlatQueueStrategy(new EndpointRouter(), testQueueEndPoint.Uri);
			openedQueue.Send(new Message(new TestMessage { Name = "ayende" }));
            Message msg = openedQueue.Peek(TimeSpan.FromSeconds(30));
            Assert.Equal(1, openedQueue.GetMessageCount());
            string msgId;
            queueStrategy.TryMoveMessage(openedQueue, msg, SubQueue.Subscriptions,out msgId);
            Assert.Equal(0, openedQueue.GetMessageCount());
        }

        #region Nested type: TestMessage

        public class TestMessage
        {
            public string Name { get; set; }
        }

        #endregion
    }
}
