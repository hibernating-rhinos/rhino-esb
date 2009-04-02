using System;
using System.Threading;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class SendAndReceiveMessages : MsmqTestBase
    {
        [Fact]
        public void Can_send_and_receive_messages()
        {
            TestMessage receivedMsg = null;
            var waitHandle = new ManualResetEvent(false);
            var today = DateTime.Today;

            Transport.MessageArrived += msg =>
            {
                receivedMsg = (TestMessage)msg.Message;
                waitHandle.Set();
                return true;
            };
            Transport.Send(TestQueueUri, new object[]
            {
                new TestMessage
                {
                    Count = 1,
                    Name = "ayende",
                    SendAt = today
                }
            });
            waitHandle.WaitOne(TimeSpan.FromSeconds(30), false);

            Assert.NotNull(receivedMsg);
            Assert.Equal(1, receivedMsg.Count);
            Assert.Equal("ayende", receivedMsg.Name);
            Assert.Equal(today, receivedMsg.SendAt);
        }

        public class TestMessage
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public DateTime SendAt { get; set; }
        }
    }
    public class SendAndReceiveMessages_FlatQueue : MsmqFlatQueueTestBase
    {
        [Fact]
        public void Can_send_and_receive_messages()
        {
            TestMessage receivedMsg = null;
            var waitHandle = new ManualResetEvent(false);
            var today = DateTime.Today;

            Transport.MessageArrived += msg =>
            {
                receivedMsg = (TestMessage)msg.Message;
                waitHandle.Set();
                return true;
            };
            Transport.Send(testQueueEndPoint, new object[]
            {
                new TestMessage
                {
                    Count = 1,
                    Name = "ayende",
                    SendAt = today
                }
            });
            waitHandle.WaitOne(TimeSpan.FromSeconds(30), false);

            Assert.NotNull(receivedMsg);
            Assert.Equal(1, receivedMsg.Count);
            Assert.Equal("ayende", receivedMsg.Name);
            Assert.Equal(today, receivedMsg.SendAt);
        }

        public class TestMessage
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public DateTime SendAt { get; set; }
        }
    }
}
