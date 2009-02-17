using System;
using System.Threading;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using Timeout = Rhino.ServiceBus.DataStructures.Timeout;

namespace Rhino.ServiceBus.LoadBalancer
{
    public class MsmqSecondaryLoadBalancer : MsmqLoadBalancer
    {
        private readonly Uri primaryLoadBalancer;
        private volatile bool tookOverWork;
        private Timeout timeout;
        private readonly Timer checkPrimaryHearteat;
        public TimeSpan TimeoutForHeartBeatFromPrimary { get; set; }

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
            int threadCount)
            : base(serializer, queueStrategy, endpointRouter, endpoint, threadCount)
        {
            TimeoutForHeartBeatFromPrimary = TimeSpan.FromSeconds(10);
            this.primaryLoadBalancer = primaryLoadBalancer;
            checkPrimaryHearteat = new Timer(OnCheckPrimaryHeartbeat);
            tookOverWork = false;
        }

        private void OnCheckPrimaryHeartbeat(object state)
        {
            if (TransportState != TransportState.Started)
                return;

            if (timeout.CheckTimestamp() == false)
                return;

            tookOverWork = true;

            checkPrimaryHearteat.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            foreach (var queueUri in KnownEndpoints.GetValues())
            {
                SendToQueue(queueUri, new Reroute
                {
                    NewEndPoint = queueUri,
                    OriginalEndPoint = PrimaryLoadBalancer
                });
            }

            foreach (var queueUri in KnownWorkers.GetValues())
            {
                SendToQueue(queueUri,
                    new AcceptingWork
                    {
                        Endpoint = queueUri
                    });
            }

            Raise(TookOverAsActiveLoadBalancer);
        }

        protected override void BeforeStart()
        {
            timeout = new Timeout(TimeoutForHeartBeatFromPrimary);

            base.BeforeStart();
        }

        protected override void AfterStart()
        {
            base.AfterStart();
            SendToQueue(primaryLoadBalancer, new QueryForAllKnownWorkersAndEndpoints());
            StartTrackingHeartbeats();
        }

        private void StartTrackingHeartbeats()
        {
            timeout.SetHeartbeat(DateTime.Now);
            checkPrimaryHearteat.Change(TimeoutForHeartBeatFromPrimary, TimeoutForHeartBeatFromPrimary);
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
            checkPrimaryHearteat.Dispose();
        }

        protected override void HandleLoadBalancerMessages(object msg)
        {
            var heartbeat = msg as Heartbeat;
            if (heartbeat == null)
                return;

            timeout.SetHeartbeat(DateTime.Now);
            if (tookOverWork)
            {
                StartTrackingHeartbeats();
            }
        }
    }
}