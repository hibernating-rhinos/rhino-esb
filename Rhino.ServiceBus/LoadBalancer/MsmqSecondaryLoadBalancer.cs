using System;
using System.Linq;
using System.Messaging;
using System.Runtime.CompilerServices;
using System.Threading;
using log4net;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using Timeout = Rhino.ServiceBus.DataStructures.Timeout;

namespace Rhino.ServiceBus.LoadBalancer
{
    public class MsmqSecondaryLoadBalancer : MsmqLoadBalancer
    {
        private readonly object locker = new object();
        private readonly ILog logger = LogManager.GetLogger(typeof (MsmqSecondaryLoadBalancer));
        private readonly Uri primaryLoadBalancer;
        private volatile bool tookOverWork;
        private Timeout timeout;
        private Timer checkPrimaryHearteat;
        public TimeSpan TimeoutForHeartBeatFromPrimary { get; set; }
        private Uri readyForWorkQueueUriFromPrimary;

        public Uri PrimaryLoadBalancer
        {
            get { return primaryLoadBalancer; }
        }

        public bool TookOverWork
        {
            get { return tookOverWork; }
        }

        public event Action TookOverAsActiveLoadBalancer;

        public MsmqSecondaryLoadBalancer(
            IMessageSerializer serializer,
            IQueueStrategy queueStrategy,
            IEndpointRouter endpointRouter,
            Uri endpoint,
            Uri primaryLoadBalancer,
            int threadCount,
			TransactionalOptions transactional,
            IMessageBuilder<Message> messageBuilder)
			: base(serializer, queueStrategy, endpointRouter, endpoint, threadCount, transactional, messageBuilder)
        {
            TimeoutForHeartBeatFromPrimary = TimeSpan.FromSeconds(10);
            this.primaryLoadBalancer = primaryLoadBalancer;
            tookOverWork = false;
        }

        private void OnCheckPrimaryHeartbeat(object state)
        {
            lock(locker)
            {
                if (tookOverWork)
                    return;

                if (TransportState != TransportState.Started)
                    return;

                if (timeout.CheckTimestamp() == false)
                    return;

                tookOverWork = true;
            }

            checkPrimaryHearteat.Dispose();
            checkPrimaryHearteat = null;

            foreach (var queueUri in KnownEndpoints.GetValues().Except(KnownWorkers.GetValues()))
            {
                if (queueUri == primaryLoadBalancer) continue;
                logger.InfoFormat("Notifying endpoints {0} that secondary load balancer {1} is taking over from {2}",
                    queueUri, 
                    Endpoint.Uri,
                    PrimaryLoadBalancer
                    );
                SendToQueue(queueUri, new Reroute
                {
                    NewEndPoint = Endpoint.Uri,
                    OriginalEndPoint = PrimaryLoadBalancer
                });
            }
            var newEndpoint = ReadyForWorkListener != null ? ReadyForWorkListener.Endpoint.Uri : Endpoint.Uri;
            var originalEndPoint = readyForWorkQueueUriFromPrimary ?? primaryLoadBalancer;
            foreach (var queueUri in KnownWorkers.GetValues())
            {
                logger.InfoFormat("Notifying worker {0} that secondary load balancer {1} is accepting work on awating listenerQueue",
                   queueUri,
                   newEndpoint,
                   originalEndPoint
                   );

                SendToQueue(queueUri,
                    new Reroute
                    {
                        NewEndPoint = newEndpoint,
                        OriginalEndPoint = originalEndPoint
                    });
            }
		
            foreach (var queueUri in KnownWorkers.GetValues())
            {
                logger.InfoFormat("Notifying worker {0} that secondary load balancer {1} is accepting work",
                   queueUri,
                   Endpoint.Uri,
                   PrimaryLoadBalancer
                   );

                SendToQueue(queueUri,
                    new AcceptingWork
                    {
                        Endpoint = Endpoint.Uri
                    });
            }

            Raise(TookOverAsActiveLoadBalancer);
        }

        protected override void BeforeStart(OpenedQueue queue)
        {
            timeout = new Timeout(TimeoutForHeartBeatFromPrimary);

            base.BeforeStart(queue);
        }

        protected override void AfterStart(OpenedQueue queue)
        {
            base.AfterStart(queue);
            SendToQueue(primaryLoadBalancer, new QueryForAllKnownWorkersAndEndpoints());
            SendToQueue(primaryLoadBalancer, new QueryReadyForWorkQueueUri());
            StartTrackingHeartbeats();
        }

        private void StartTrackingHeartbeats()
        {
            checkPrimaryHearteat = new Timer(OnCheckPrimaryHeartbeat,null,
                TimeoutForHeartBeatFromPrimary,
                TimeoutForHeartBeatFromPrimary);
            timeout.SetHeartbeat(DateTime.Now);
            tookOverWork = false;
        }

        protected override bool ShouldNotifyWorkersLoaderIsReadyToAcceptWorkOnStartup
        {
            get
            {
                return false;
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (checkPrimaryHearteat != null)
                checkPrimaryHearteat.Dispose();
        }

        protected override void HandleLoadBalancerMessages(object msg)
        {
            var heartbeat = msg as Heartbeat;
            if (heartbeat != null)
            {
                timeout.SetHeartbeat(DateTime.Now);
                logger.Debug("Got heartbeat from primary");
                if (tookOverWork)
                {
                    logger.Info("Primary is now back in action, hurray!");
                    StartTrackingHeartbeats();
                }
            }
            var readyForWorkQueueUriMessage = msg as ReadyForWorkQueueUri;
            if (readyForWorkQueueUriMessage != null)
            {
                logger.InfoFormat("Got ReadyForWork endpoint from primary : {0}", readyForWorkQueueUriMessage.Endpoint);
                readyForWorkQueueUriFromPrimary = readyForWorkQueueUriMessage.Endpoint;
            }
            var newEndpoint = msg as NewEndpointPersisted;
            if(newEndpoint != null)
            {
                logger.InfoFormat("Got new endpoint persisted event from primary: {0}", newEndpoint.PersistedEndpoint);
                KnownEndpoints.Add(newEndpoint.PersistedEndpoint);
            }
            var newWorker = msg as NewWorkerPersisted;
            if(newWorker != null)
            {
                logger.InfoFormat("Got new worker persisted event from primary: {0}", newWorker.Endpoint);
                KnownWorkers.Add(newWorker.Endpoint);
            }
        }
    }
}