using System;
using System.Messaging;
using Castle.MicroKernel;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Msmq.TransportActions;
using Rhino.ServiceBus.Serializers;

namespace Rhino.ServiceBus.Tests
{
    public class MsmqTestBase : IDisposable
    {
        private readonly string subbscriptionQueuePath;
        protected readonly Endpoint SubscriptionsUri;

        protected readonly string testQueuePath;
        protected readonly Endpoint TestQueueUri;

        protected readonly string testQueuePath2;
        protected readonly Endpoint TestQueueUri2;

        protected readonly string transactionalTestQueuePath;
        protected readonly Endpoint TransactionalTestQueueUri;

        protected MessageQueue queue;
        protected MessageQueue subscriptions;
        protected MessageQueue transactionalQueue;

        private ITransport transactionalTransport;
        private ITransport transport;
        protected readonly MessageQueue testQueue2;
        private readonly string subbscriptionQueuePath2;

        public MsmqTestBase()
        {
            TestQueueUri = new Uri("msmq://localhost/test_queue").ToEndpoint();
            testQueuePath = MsmqUtil.GetQueuePath(TestQueueUri).QueuePath;

            TestQueueUri2 = new Uri("msmq://localhost/test_queue2").ToEndpoint();
			testQueuePath2 = MsmqUtil.GetQueuePath(TestQueueUri2).QueuePath;

            TransactionalTestQueueUri = new Uri("msmq://localhost/transactional_test_queue").ToEndpoint();
			transactionalTestQueuePath = MsmqUtil.GetQueuePath(TransactionalTestQueueUri).QueuePath;

            SubscriptionsUri2 = new Uri("msmq://localhost/test_queue2;subscriptions").ToEndpoint();
			subbscriptionQueuePath2 = MsmqUtil.GetQueuePath(SubscriptionsUri2).QueuePathWithSubQueue;

            SubscriptionsUri = new Uri("msmq://localhost/test_queue;subscriptions").ToEndpoint();
			subbscriptionQueuePath = MsmqUtil.GetQueuePath(SubscriptionsUri).QueuePathWithSubQueue;

            if (MessageQueue.Exists(testQueuePath) == false)
                MessageQueue.Create(testQueuePath);

            if (MessageQueue.Exists(testQueuePath2) == false)
                MessageQueue.Create(testQueuePath2);

            if (MessageQueue.Exists(transactionalTestQueuePath) == false)
                MessageQueue.Create(transactionalTestQueuePath, true);

            queue = new MessageQueue(testQueuePath);
            queue.Purge();

            using (var errQueue = new MessageQueue(testQueuePath + ";errors"))
            {
                errQueue.Purge();
            }

            testQueue2 = new MessageQueue(testQueuePath2);
            testQueue2.Purge();

            using (var errQueue2 = new MessageQueue(testQueuePath2 + ";errors"))
            {
                errQueue2.Purge();
            }

            transactionalQueue = new MessageQueue(transactionalTestQueuePath);
            transactionalQueue.Purge();

            using (var errQueue3 = new MessageQueue(transactionalTestQueuePath + ";errors"))
            {
                errQueue3.Purge();
            }

            using (var discardedQueue = new MessageQueue(testQueuePath + ";discarded"))
            {
                discardedQueue.Purge();
            }

			using (var timeoutQueue = new MessageQueue(testQueuePath + ";timeout"))
			{
				timeoutQueue.Purge();
			}

            subscriptions = new MessageQueue(subbscriptionQueuePath)
            {
                Formatter = new XmlMessageFormatter(new[] { typeof(string) })
            };
            subscriptions.Purge();

            using(var subscriptions2 = new MessageQueue(subbscriptionQueuePath2))
            {
                subscriptions2.Purge();
            }
        }

        public Endpoint SubscriptionsUri2 { get; set; }

        public ITransport Transport
        {
            get
            {
                if (transport == null)
                {
                    transport = new MsmqTransport(
                        new XmlMessageSerializer(
                        	new DefaultReflection(), 
                        	new DefaultKernel()), 
                            new SubQueueStrategy(),
                            TestQueueUri.Uri, 1, 
                            defaultTransportActions,
                            new EndpointRouter());
                    transport.Start();
                }
                return transport;
            }
        }

        public ITransport TransactionalTransport
        {
            get
            {
                if (transactionalTransport == null)
                {
                    transactionalTransport = new MsmqTransport(
                        new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel()), 
                        new SubQueueStrategy(),
                        TransactionalTestQueueUri.Uri, 
                        1,
                        defaultTransportActions,
                            new EndpointRouter());
                    transactionalTransport.Start();
                }
                return transactionalTransport;
            }
        }

        private static ITransportAction[] defaultTransportActions
        {
            get
            {
                var qs = new SubQueueStrategy();
                return new ITransportAction[]
                {
                    new AdministrativeAction(),
                    new ErrorAction(5, qs),
                    new ShutDownAction(),
                    new TimeoutAction(qs)
                };
            }
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            queue.Dispose();
            transactionalQueue.Dispose();
            subscriptions.Dispose();

            if (transport != null)
                transport.Dispose();
            if (transactionalTransport != null)
                transactionalTransport.Dispose();
        }

        #endregion
    }
}
