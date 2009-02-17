using System;
using System.Messaging;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class UnserializableMessageWillBeForwardedToErrorQueue : MsmqTestBase
    {
        [Fact]
        public void Message_send_should_get_routed_to_error_queue()
        {
            object o1 = null;
            
            Transport.MessageArrived += o =>
            {
                o1 = o;
                return true;
            };
            queue.Send("blah blah not valid");

            using (var errorQueue = new MessageQueue(testQueuePath + ";errors"))
            {
                var errMsg = errorQueue.Receive(TimeSpan.FromSeconds(30));
                Assert.NotNull(errMsg);
                Assert.Null(o1);
            }
        }

        [Fact]
        public void Should_raise_event_with_no_id()
        {
            bool wasCalled = false;
            Transport.MessageSerializationException += (info,exception) => wasCalled = true;
            queue.Send("blah blah not valid");

            using (var errorQueue = new MessageQueue(testQueuePath + ";errors"))
            {
                errorQueue.Receive(TimeSpan.FromSeconds(30));// wait for message to be processed.
            }

            Assert.True(wasCalled);
        }

        [Fact]
        public void Should_raise_event()
        {
            bool wasCalled = false;
            Transport.MessageSerializationException += (info, exception) => wasCalled = true;
            queue.Send(new Message
            {
                Body = "blah blah not valid",
                Extension = Guid.NewGuid().ToByteArray()
            });

            using (var errorQueue = new MessageQueue(testQueuePath + ";errors"))
            {
                errorQueue.Receive(TimeSpan.FromSeconds(30));// wait for message to be processed.
            }

            Assert.True(wasCalled);
        }
    }
    public class UnserializableMessageWillBeForwardedToFlattenedErrorQueue : MsmqFlatQueueTestBase
    {
        [Fact]
        public void Message_send_should_get_routed_to_error_queue()
        {
            object o1 = null;

            Transport.MessageArrived += o =>
            {
                o1 = o;
                return true;
            };
			queue.Send(new Message("blah blah not valid"));

            using (var errorQueue = new MessageQueue(testQueuePath + "#errors"))
            {
                var errMsg = errorQueue.Receive(TimeSpan.FromSeconds(30));
                Assert.NotNull(errMsg);
                Assert.Null(o1);
            }
        }

        [Fact]
        public void Should_raise_event_with_no_id()
        {
            bool wasCalled = false;
            Transport.MessageSerializationException += (info, exception) => wasCalled = true;
			queue.Send(new Message("blah blah not valid"));

            using (var errorQueue = new MessageQueue(testQueuePath + "#errors"))
            {
                errorQueue.Receive(TimeSpan.FromSeconds(30));// wait for message to be processed.
            }

            Assert.True(wasCalled);
        }

        [Fact]
        public void Should_raise_event()
        {
            bool wasCalled = false;
            Transport.MessageSerializationException += (info, exception) => wasCalled = true;
            queue.Send(new Message
            {
                Body = "blah blah not valid",
                Extension = Guid.NewGuid().ToByteArray()
            });

            using (var errorQueue = new MessageQueue(testQueuePath + "#errors"))
            {
                errorQueue.Receive(TimeSpan.FromSeconds(30));// wait for message to be processed.
            }

            Assert.True(wasCalled);
        }
    }
}
