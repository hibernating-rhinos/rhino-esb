using System;
using System.Messaging;
using System.Threading;
using System.Transactions;
using Xunit;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Tests
{
    public class MsmqBehaviorTests : MsmqTestBase
    {
        public MsmqBehaviorTests()
        {
            using (var subqueue = new MessageQueue(testQueuePath + ";errors"))
            {
                subqueue.Purge();
            }

            using (var subqueue = new MessageQueue(transactionalTestQueuePath + ";errors"))
            {
                subqueue.Purge();
            }
        }

        [Fact]
        public void Can_receive_message_from_non_transactional_queue_while_specifying_transaction_single()
        {
            queue.Send("a");

            var msg = queue.Receive(MessageQueueTransactionType.Single);
            Assert.Equal("a", msg.Body);
            var count = queue.GetCount();
            Assert.Equal(0, count);
        }

        [Fact]
        public void Can_move_message_to_sub_queue()
        {
            queue.Send("a");

            var peek = queue.Peek(TimeSpan.FromSeconds(30));
            queue.MoveToSubQueue("errors", peek);

            using (var subqueue = new MessageQueue(testQueuePath + ";errors"))
            {
                subqueue.Formatter = new XmlMessageFormatter(new[] { typeof(string) });
                var receive = subqueue.Receive(TimeSpan.FromSeconds(30));
                Assert.Equal("a", receive.Body);
            }
        }

        [Fact]
        public void Peeking_twice()
        {
            queue.Send("a");

            var peek1 = queue.Peek(TimeSpan.FromSeconds(30));

            var peek2 = queue.Peek(TimeSpan.FromSeconds(30));

            Assert.Equal(peek1.Id, peek2.Id);
        }

        [Fact]
        public void Consuming_using_enumerator()
        {
            queue.Send("a");
            queue.Send("b");
            queue.Send("c");

            var enumerator2 = queue.GetMessageEnumerator2();
            try
            {
                enumerator2.MoveNext();
                Assert.Equal("a", enumerator2.RemoveCurrent().Body);
                Assert.Equal("b", enumerator2.RemoveCurrent().Body);
                Assert.Equal("c", enumerator2.RemoveCurrent().Body);
            }
            finally
            {
                enumerator2.Close();
            }
        }


        [Fact]
        public void Peeking_twice_different_queue_isntances_on_same_queue()
        {
            queue.Send("a");

            var peek1 = queue.Peek(TimeSpan.FromSeconds(30));

            using(var q2 = new MessageQueue(testQueuePath,QueueAccessMode.Receive))
            {
                var peek2 = q2.Peek(TimeSpan.FromSeconds(30));
                Assert.Equal(peek1.Id, peek2.Id);
            }
        }

        [Fact]
        public void Competing_consumers()
        {
            queue.Send("a");

            var peek1 = queue.Peek(TimeSpan.FromSeconds(30));

            using (var q2 = new MessageQueue(testQueuePath, QueueAccessMode.Receive))
            {
                var peek2 = q2.Peek(TimeSpan.FromSeconds(30));
                q2.ReceiveById(peek2.Id);
            }

            Assert.Throws<InvalidOperationException>(()=>queue.ReceiveById(peek1.Id))
            ;
        }

        [Fact]
        public void Moving_to_subqueue_will_take_part_in_ambient_transaction()
        {
            queue.Send("a");

            var peek = queue.Peek(TimeSpan.FromSeconds(30));
            using (var tx = new TransactionScope())
            {
                queue.MoveToSubQueue("errors", peek);
                tx.Complete();
            }

            using (var subqueue = new MessageQueue(testQueuePath + ";errors"))
            {
                subqueue.Formatter = new XmlMessageFormatter(new[] { typeof(string) });
                var receive = subqueue.Receive(TimeSpan.FromSeconds(30));
                Assert.Equal("a", receive.Body);
            }
        }

        [Fact]
        public void Moving_to_subqueue_will_take_part_in_ambient_transaction_and_when_rolled_back_will_cancel_move()
        {
            using (var tx = new TransactionScope())
            {
                transactionalQueue.Send("a",MessageQueueTransactionType.Automatic);
                tx.Complete();
            }
            
            using (new TransactionScope())
            {
                var peek = transactionalQueue.Peek(TimeSpan.FromSeconds(30));
                transactionalQueue.MoveToSubQueue("errors", peek);
                //tx.Complete();
            }

            using (var subqueue = new MessageQueue(transactionalTestQueuePath + ";errors"))
            {
                subqueue.Formatter = new XmlMessageFormatter(new[] { typeof(string) });
                Assert.Equal(0, subqueue.GetAllMessages().Length);
            }
        }

        [Fact]
        public void Can_rollback_message_to_transactional_queue()
        {
            using (var tx = new TransactionScope())
            {
                transactionalQueue.Send("foo", MessageQueueTransactionType.Automatic);
                tx.Complete();
            }

            Assert.NotNull(transactionalQueue.Peek(TimeSpan.FromSeconds(1)));

            using (new TransactionScope())
            {
                transactionalQueue.Receive(TimeSpan.FromSeconds(1), MessageQueueTransactionType.Automatic);
                //do not complete tx
            }

            var peek = transactionalQueue.Peek(TimeSpan.FromMilliseconds(1000));
            Assert.NotNull(peek);
        }

        [Fact]
        public void Can_use_enumerator_to_go_through_all_messages()
        {
            queue.Send("a");
            queue.Send("b");
            queue.Send("c");

            var enumerator2 = queue.GetMessageEnumerator2();
            Assert.True(enumerator2.MoveNext(TimeSpan.FromSeconds(0)));
            Assert.True(enumerator2.MoveNext(TimeSpan.FromSeconds(0)));
            Assert.True(enumerator2.MoveNext(TimeSpan.FromSeconds(0)));
            Assert.False(enumerator2.MoveNext(TimeSpan.FromSeconds(0)));
        }

        [Fact]
        public void When_begin_peeking_for_messages_will_get_one_per_message()
        {
            int count = 0;
            var wait = new ManualResetEvent(false);
            queue.BeginPeek(TimeSpan.FromSeconds(1), null, delegate(IAsyncResult ar)
            {
                Interlocked.Increment(ref count);
                queue.EndPeek(ar);
                wait.Set();
            });

            queue.Send("test1", MessageQueueTransactionType.None);
            queue.Send("test2", MessageQueueTransactionType.None);

            wait.WaitOne(TimeSpan.FromSeconds(30));

            Assert.Equal(1, count);
        }

        [Fact]
        public void Can_call_begin_peek_from_begin_peek()
        {
            int count = 0;
            var wait = new ManualResetEvent(false);

            queue.BeginPeek(TimeSpan.FromSeconds(1), null, delegate(IAsyncResult ar)
            {
                Interlocked.Increment(ref count);
                queue.EndPeek(ar);

                queue.BeginPeek(TimeSpan.FromSeconds(1), null, delegate(IAsyncResult ar2)
                {
                    Interlocked.Increment(ref count);
                    queue.EndPeek(ar2);
                    wait.Set();
                });
            });

            queue.Send("test1");
            queue.Send("test2");
            wait.WaitOne(TimeSpan.FromSeconds(30));

            Assert.Equal(2, count);
        }

        [Fact]
        public void Trying_to_send_message_with_large_label()
        {
            queue.Send(new Message
            {
                Label = new string('a', 249),
                Body = "send"
            });
        }

        [Fact]
        public void When_peeking_and_there_is_no_message()
        {
            IAsyncResult asyncResult = queue.BeginPeek(
                TimeSpan.FromMilliseconds(1), null, delegate { });
            asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(30));

            Assert.False(asyncResult.CompletedSynchronously);

            Assert.Throws<MessageQueueException>(
                "Timeout for the requested operation has expired.",
                () => queue.EndPeek(asyncResult));
        }


        [Fact]
        public void When_peeking_and_there_is_no_message_should_get_the_perfect_code()
        {
            IAsyncResult asyncResult = queue.BeginPeek(
                TimeSpan.FromMilliseconds(1), null, delegate { });
            asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(30));

            Assert.False(asyncResult.CompletedSynchronously);

            MessageQueueErrorCode errorCode = 0;
            try
            {
                queue.EndPeek(asyncResult);
                Assert.False(true, "should not get this");
            }
            catch (MessageQueueException e)
            {
                errorCode = e.MessageQueueErrorCode;
            }

            Assert.Equal(MessageQueueErrorCode.IOTimeout, errorCode);
        }

        [Fact]
        public void Count_will_count_items_in_subqueue()
        {
            using(var errors = new MessageQueue(testQueuePath + ";errors"))
            {
                errors.Purge();
                
                queue.Send("a");
                queue.Send("b");

                var msg = queue.Peek(TimeSpan.FromSeconds(30));
                queue.MoveToSubQueue("errors", msg);

                var count = queue.GetCount();
                
                Assert.Equal(2, count);
            }
        }
    }
}
