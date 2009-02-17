using System;
using System.Messaging;
using System.Threading;
using System.Transactions;
using log4net;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using MessageType = Rhino.ServiceBus.Msmq.MessageType;
using System.Linq;

namespace Rhino.ServiceBus.LoadBalancer
{
    public class MsmqLoadBalancer : AbstractMsmqListener
    {
        public Uri SecondaryLoadBalancer { get; set; }
        private readonly IQueueStrategy queueStrategy;
        private readonly ILog logger = LogManager.GetLogger(typeof(MsmqLoadBalancer));

        private readonly Queue<Uri> readyForWork = new Queue<Uri>();
        private readonly Set<Uri> knownWorkers = new Set<Uri>();
        private readonly Timer heartBeatTimer;
        private readonly Set<Uri> knownEndpoints = new Set<Uri>();

        public event Action<Message> MessageBatchSentToAllWorkers;
        public event Action SentNewWorkerPersisted;
        public event Action SentNewEndpointPersisted;

        public MsmqLoadBalancer(
            IMessageSerializer serializer,
            IQueueStrategy queueStrategy,
            IEndpointRouter endpointRouter,
            Uri endpoint,
            int threadCount)
            : base(queueStrategy, endpoint, threadCount, serializer, endpointRouter)
        {
            heartBeatTimer = new Timer(SendHeartBeatToSecondaryServer);
            this.queueStrategy = queueStrategy;
        }

        protected void SendHeartBeatToSecondaryServer(object ignored)
        {
            SendToQueue(SecondaryLoadBalancer, new Heartbeat
            {
                From = Endpoint.Uri,
                At = DateTime.Now,
            });
        }

        public Set<Uri> KnownWorkers
        {
            get { return knownWorkers; }
        }

        public Set<Uri> KnownEndpoints
        {
            get { return knownEndpoints; }
        }

        public int NumberOfWorkersReadyToHandleMessages
        {
            get { return readyForWork.TotalCount; }
        }

        protected override void BeforeStart()
        {
            try
            {
                queueStrategy.InitializeQueue(Endpoint);
            }
            catch (Exception e)
            {
                throw new TransportException(
                    "Could not open queue for load balancer: " + Endpoint + Environment.NewLine +
                    "Queue path: " + MsmqUtil.GetQueuePath(Endpoint), e);
            }

            ReadUrisFromSubQueue(KnownWorkers, SubQueue.Workers);

            ReadUrisFromSubQueue(KnownEndpoints, SubQueue.Endpoints);

            RemoveAllReadyToWorkMessages();
        }

        private void ReadUrisFromSubQueue(Set<Uri> set, SubQueue subQueue)
        {
			using (var q  = MsmqUtil.GetQueuePath(Endpoint).Open(QueueAccessMode.Receive))
            using (var sq = q.OpenSubQueue(subQueue, QueueAccessMode.SendAndReceive))
            {
                var messages = sq.GetAllMessagesWithStringFormatter();
                foreach (var message in messages)
                {
                    var uriString = message.Body.ToString();
                    set.Add(new Uri(uriString));
                }
            }
        }

        private void RemoveAllReadyToWorkMessages()
        {
            using (var tx = new TransactionScope())
            using (var readyForWorkQueue = MsmqUtil.GetQueuePath(Endpoint).Open(QueueAccessMode.Receive))
            using (var enumerator = readyForWorkQueue.GetMessageEnumerator2())
            {
                try
                {
                    while (enumerator.MoveNext())
                    {
                        while (
                            enumerator.Current != null &&
                            enumerator.Current.Label == typeof(ReadyToWork).FullName)
                        {
                            var current = enumerator.RemoveCurrent(readyForWorkQueue.GetTransactionType());
                            HandleLoadBalancerMessage(current);
                        }
                    }
                }
                catch (MessageQueueException e)
                {
                    if (e.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                        throw;
                }
                readyForWork.Clear();
                tx.Complete();
            }
        }

        protected override void AfterStart()
        {
            if (SecondaryLoadBalancer != null)
            {
                SendHeartBeatToSecondaryServer(null);
                heartBeatTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            if (ShouldNotifyWorkersLoaderIsReadyToAcceptWorkOnStartup)
                NotifyWorkersThatLoaderIsReadyToAcceptWork();
        }

        protected virtual bool ShouldNotifyWorkersLoaderIsReadyToAcceptWorkOnStartup
        {
            get
            {
                return true;
            }
        }

        protected void NotifyWorkersThatLoaderIsReadyToAcceptWork()
        {
            var acceptingWork = new AcceptingWork { Endpoint = Endpoint.Uri };
            SendToAllWorkers(
                GenerateMsmqMessageFromMessageBatch(acceptingWork)
                );
        }

        protected override void OnStop()
        {
            heartBeatTimer.Dispose();
        }

        protected override void HandlePeekedMessage(Message message)
        {
            try
            {
                using (var tx = new TransactionScope(TransactionScopeOption.Required, GetTransactionTimeout()))
                {
                    message = queue.TryGetMessageFromQueue(message.Id);
                    if (message == null)
                        return;

                    PersistEndpoint(message);

                    switch ((MessageType)message.AppSpecific)
                    {
                        case MessageType.LoadBalancerMessageMarker:
                            HandleLoadBalancerMessage(message);
                            break;
                        case MessageType.AdministrativeMessageMarker:
                            SendToAllWorkers(message);
                            break;
                        default:
                            HandleStandardMessage(message);
                            break;
                    }
                    tx.Complete();
                }
            }
            catch (Exception e)
            {
                logger.Error("Fail to process load balanced message properly", e);
            }
        }

        private void PersistEndpoint(Message message)
        {
            var queueUri = MsmqUtil.GetQueueUri(message.ResponseQueue);
            if (queueUri == null)
                return;
            bool needToPersist = knownEndpoints.Add(queueUri);
            if (needToPersist == false)
                return;
            var persistedEndPoint = new Message
            {
                Formatter = new XmlMessageFormatter(new[] { typeof(string) }),
                Body = queueUri.ToString(),
                Label = ("Known end point: " + queueUri).EnsureLabelLength()
            };
            queue.Send(persistedEndPoint.SetSubQueueToSendTo(SubQueue.Endpoints));

            SendToQueue(SecondaryLoadBalancer, new NewEndpointPersisted
            {
                PersistedEndpoint = queueUri
            });
            Raise(SentNewEndpointPersisted);
        }

        protected void SendToQueue(Uri queueUri, params object[] msgs)
        {
            if (queueUri == null)
                return;

            try
            {
                using (var secondaryLoadBalancerQueue = MsmqUtil.GetQueuePath(new Endpoint { Uri = queueUri }).Open(QueueAccessMode.Send))
                {
                    secondaryLoadBalancerQueue.Send(GenerateMsmqMessageFromMessageBatch(msgs));
                }
            }
            catch (Exception e)
            {
                throw new LoadBalancerException("Could not send message to queue: " + queueUri, e);
            }
        }

        private void HandleStandardMessage(Message message)
        {
            var worker = readyForWork.Dequeue();

            if (worker == null) // handle message later
            {
                queue.Send(message);
            }
            else
            {
                var workerEndpoint = endpointRouter.GetRoutedEndpoint(worker);
				using (var workerQueue = MsmqUtil.GetQueuePath(workerEndpoint).Open(QueueAccessMode.Send))
                {
                    workerQueue.Send(message);
                }
            }
        }

        private void SendToAllWorkers(Message message)
        {
            var values = KnownWorkers.GetValues();
            foreach (var worker in values)
            {
                var workerEndpoint = endpointRouter.GetRoutedEndpoint(worker);
				using (var workerQueue = MsmqUtil.GetQueuePath(workerEndpoint).Open(QueueAccessMode.Send))
                {
                    workerQueue.Send(message);
                }
            }
            if (values.Length == 0)
                return;

            var copy = MessageBatchSentToAllWorkers;
            if (copy != null)
                copy(message);
        }

        private void HandleLoadBalancerMessage(Message message)
        {
            foreach (var msg in DeserializeMessages(queue, message, null))
            {
                var query = msg as QueryForAllKnownWorkersAndEndpoints;
                if (query != null)
                {
                    SendKnownWorkersAndKnownEndpoints(message.ResponseQueue);
                    continue;
                }

                var work = msg as ReadyToWork;
                if (work != null)
                {
                    var needToAddToQueue = KnownWorkers.Add(work.Endpoint);

                    if (needToAddToQueue)
                        AddWorkerToQueue(work);

                    readyForWork.Enqueue(work.Endpoint);
                }

                HandleLoadBalancerMessages(msg);
            }
        }

        private void SendKnownWorkersAndKnownEndpoints(MessageQueue responseQueue)
        {
            if (responseQueue == null)
                return;
            try
            {
                var endpoints = KnownEndpoints.GetValues();
                var workers = KnownWorkers.GetValues();

                var index = 0;
                while (index < endpoints.Length)
                {
                    var endpointsBatch = endpoints
                        .Skip(index)
                        .Take(256)
                        .Select(x => new NewEndpointPersisted { PersistedEndpoint = x })
                        .ToArray();
                    index += endpointsBatch.Length;

                    responseQueue.Send(GenerateMsmqMessageFromMessageBatch(endpointsBatch));
                }

                index = 0;
                while (index < workers.Length)
                {
                    var workersBatch = workers
                        .Skip(index)
                        .Take(256)
                        .Select(x => new NewWorkerPersisted { Endpoint = x })
                        .ToArray();
                    index += workersBatch.Length;

                    responseQueue.Send(GenerateMsmqMessageFromMessageBatch(workersBatch));
                }
            }
            catch (Exception e)
            {
                logger.Error("Failed to send known endpoints and known workers", e);
            }
        }

        protected virtual void HandleLoadBalancerMessages(object msg)
        {
        }

        private void AddWorkerToQueue(ReadyToWork work)
        {
            var persistedWorker = new Message
            {
                Formatter = new XmlMessageFormatter(new[] { typeof(string) }),
                Body = work.Endpoint.ToString(),
                Label = ("Known worker: " + work.Endpoint).EnsureLabelLength()
            };
            queue.Send(persistedWorker.SetSubQueueToSendTo(SubQueue.Workers));

            SendToQueue(SecondaryLoadBalancer, new NewWorkerPersisted
            {
                Endpoint = work.Endpoint
            });
            Raise(SentNewWorkerPersisted);
        }
    }
}
