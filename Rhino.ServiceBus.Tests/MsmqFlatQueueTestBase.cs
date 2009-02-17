using System;
using System.Collections.Generic;
using System.Messaging;
using Castle.MicroKernel;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Msmq.TransportActions;
using Rhino.ServiceBus.Serializers;

namespace Rhino.ServiceBus.Tests
{
    public class MsmqFlatQueueTestBase : IDisposable
    {
        private readonly string subscriptionQueuePath;
        protected readonly Endpoint subscriptionsEndpoint;

        protected readonly string testQueuePath;
        protected readonly Endpoint testQueueEndPoint;
        
        protected readonly string transactionalTestQueuePath;
        protected readonly Endpoint transactionalTestQueueEndpoint;

        protected OpenedQueue queue;
		protected OpenedQueue subscriptions;
		protected OpenedQueue transactionalQueue;

        private ITransport transactionalTransport;
        private ITransport transport;

        /// <summary>
        /// we use this to initalize the defaults for the test
        /// </summary>
        private readonly MsmqTestBase defaultTestBase;

        public MsmqFlatQueueTestBase()
        {
            defaultTestBase = new MsmqTestBase();

            testQueueEndPoint = new Endpoint
            {
                Uri = new Uri("msmq://localhost/test_queue")
            };
            testQueuePath = MsmqUtil.GetQueuePath(testQueueEndPoint).QueuePath;

            transactionalTestQueueEndpoint = new Endpoint
            {
                Uri = new Uri("msmq://localhost/transactional_test_queue")
            };
			transactionalTestQueuePath = MsmqUtil.GetQueuePath(transactionalTestQueueEndpoint).QueuePath;

            subscriptionsEndpoint = new Endpoint
            {
                Uri = new Uri(testQueueEndPoint.Uri + "#" + subscriptions)
            };
			subscriptionQueuePath = MsmqUtil.GetQueuePath(subscriptionsEndpoint).QueuePath;


			SetupQueues();

        	queue = MsmqUtil.GetQueuePath(testQueueEndPoint).Open();
        	transactionalQueue = MsmqUtil.GetQueuePath(transactionalTestQueueEndpoint).Open();
        	subscriptions = MsmqUtil.GetQueuePath(subscriptionsEndpoint).Open(QueueAccessMode.SendAndReceive,
				new XmlMessageFormatter(new[]
				{
					typeof (string)
				}));
        }
		
		private void SetupQueues()
		{
			ForEachQueuePath((path,transactional)=>
			             	{
								if (MessageQueue.Exists(path) == false)
								{
									MessageQueue.Create(path, transactional);
								}
								else
								{
									using (var cue = new MessageQueue(path))
									{
										cue.Purge();
									}
								}    		
			             	});
		}
		private void DeleteQueues()
		{
			ForEachQueuePath((path, transactional) =>
			{
				if (MessageQueue.Exists(path) )
				{
					MessageQueue.Delete(path);
				}
			});
		}
		private void ForEachQueuePath(Action<string,bool> action)
		{
			var rootUris = new Dictionary<string, bool>
			           	{
			           		{testQueuePath, false},
			           		{transactionalTestQueuePath, true}
			           	};
			var sub = new[] { "errors", "subscriptions", "discarded", "timeout" };
			foreach (var pair in rootUris)
			{
				foreach (var s in sub)
				{
					var path= pair.Key + "#" + s;
					action(path, pair.Value);
				}

			}
		}

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
                            new FlatQueueStrategy(new EndpointRouter(),testQueueEndPoint.Uri),
                            testQueueEndPoint.Uri, 1,
                        DefaultTransportActions(testQueueEndPoint.Uri),
                            new EndpointRouter());
                    transport.Start();
                }
                return transport;
            }
        }

        private static ITransportAction[] DefaultTransportActions(Uri endpoint)
        {
            var qs = new FlatQueueStrategy(new EndpointRouter(),endpoint);
            return new ITransportAction[]
            {
                new AdministrativeAction(),
                new ErrorAction(5, qs),
                new ShutDownAction(),
                new TimeoutAction(qs)
            };
        }

        public ITransport TransactionalTransport
        {
            get
            {
                if (transactionalTransport == null)
                {
                    transactionalTransport = new MsmqTransport(
                        new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel()),
                        new FlatQueueStrategy(new EndpointRouter(),transactionalTestQueueEndpoint.Uri),
                        transactionalTestQueueEndpoint.Uri, 1, DefaultTransportActions(transactionalTestQueueEndpoint.Uri),
                            new EndpointRouter());
                    transactionalTransport.Start();
                }
                return transactionalTransport;
            }
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            defaultTestBase.Dispose();

            queue.Dispose();
            transactionalQueue.Dispose();
            subscriptions.Dispose();

            if (transport != null)
                transport.Dispose();
            if (transactionalTransport != null)
                transactionalTransport.Dispose();

			DeleteQueues();
        }

        #endregion
    }
}
