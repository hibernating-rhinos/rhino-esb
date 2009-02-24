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
        private readonly Uri secondaryLoadBalancer;
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

        public MsmqLoadBalancer(
                    IMessageSerializer serializer,
                    IQueueStrategy queueStrategy,
                    IEndpointRouter endpointRouter,
                    Uri endpoint,
                    int threadCount,
                    Uri secondaryLoadBalancer)
            : this(serializer ,queueStrategy, endpointRouter,endpoint, threadCount)
        {
            this.secondaryLoadBalancer = secondaryLoadBalancer;
        }

        protected void SendHeartBeatToSecondaryServer(object ignored)
        {
            SendToQueue(secondaryLoadBalancer, new Heartbeat
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

        protected override void BeforeStart(OpenedQueue queue)
        {
            try
            {
                queueStrategy.InitializeQueue(Endpoint, QueueType.LoadBalancer);
            }
            catch (Exception e)
            {
                throw new TransportException(
                    "Could not open queue for load balancer: " + Endpoint + Environment.NewLine +
                    "Queue path: " + MsmqUtil.GetQueuePath(Endpoint), e);
            }

            try
            {
                ReadUrisFromSubQueue(KnownWorkers, SubQueue.Workers);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Could not read workers subqueue", e);
            }

            try
            {
                ReadUrisFromSubQueue(KnownEndpoints, SubQueue.Endpoints);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Could not read endpoints subqueue", e);
            }

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
            using (var readyForWorkQueue = MsmqUtil.GetQueuePath(Endpoint).Open(QueueAccessMode.SendAndReceive))
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
                            HandleLoadBalancerMessage(readyForWorkQueue,current);
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

        protected override void AfterStart(OpenedQueue queue)
        {
            if (secondaryLoadBalancer != null)
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
                GenerateMsmqMessageFromMessageBatch(acceptingWork),
                "Notifing {1} that {1} is accepting work"
                );
        }

        protected override void OnStop()
        {
            heartBeatTimer.Dispose();
        }

        protected override void HandlePeekedMessage(OpenedQueue queue, Message message)
        {
            try
            {
                using (var tx = new TransactionScope(TransactionScopeOption.Required, GetTransactionTimeout()))
                {
                    message = queue.TryGetMessageFromQueue(message.Id);
                    if (message == null)
                        return;

                    PersistEndpoint(queue, message);

                    switch ((MessageType)message.AppSpecific)
                    {
                        case MessageType.ShutDownMessageMarker:
                            //silently cnsume the message
                            break;
                        case MessageType.LoadBalancerMessageMarker:
                            HandleLoadBalancerMessage(queue,message);
                            break;
                        case MessageType.AdministrativeMessageMarker:
                            SendToAllWorkers(message,"Dispatching administrative message from {0} to load balancer {1}");
                            break;
                        default:
                            HandleStandardMessage(queue,message);
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

        private void PersistEndpoint(OpenedQueue queue, Message message)
        {
            var queueUri = MsmqUtil.GetQueueUri(message.ResponseQueue);
            if (queueUri == null)
                return;
            bool needToPersist = knownEndpoints.Add(queueUri);
            if (needToPersist == false)
                return;
            
            logger.InfoFormat("Adding new endpoint: {0}",queueUri);
            var persistedEndPoint = new Message
            {
                Formatter = new XmlMessageFormatter(new[] { typeof(string) }),
                Body = queueUri.ToString(),
                Label = ("Known end point: " + queueUri).EnsureLabelLength()
            };
            queue.Send(persistedEndPoint.SetSubQueueToSendTo(SubQueue.Endpoints));

            SendToQueue(secondaryLoadBalancer, new NewEndpointPersisted
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
                var queueInfo = MsmqUtil.GetQueuePath(new Endpoint { Uri = queueUri });
                using (var secondaryLoadBalancerQueue = queueInfo.Open(QueueAccessMode.Send))
                {
                    secondaryLoadBalancerQueue.Send(GenerateMsmqMessageFromMessageBatch(msgs));
                }
            }
            catch (Exception e)
            {
                throw new LoadBalancerException("Could not send message to queue: " + queueUri, e);
            }
        }

        private void HandleStandardMessage(OpenedQueue queue, Message message)
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
                    logger.DebugFormat("Dispatching message '{0}' to {1}", message.Id, workerEndpoint.Uri);
                    workerQueue.Send(message);
                }
            }
        }

        private void SendToAllWorkers(Message message, string logMessage)
        {
            var values = KnownWorkers.GetValues();
            foreach (var worker in values)
            {
                var workerEndpoint = endpointRouter.GetRoutedEndpoint(worker);
				using (var workerQueue = MsmqUtil.GetQueuePath(workerEndpoint).Open(QueueAccessMode.Send))
                {
                    logger.DebugFormat(logMessage, Endpoint.Uri, worker);
                    workerQueue.Send(message);
                }
            }
            if (values.Length == 0)
                return;

            var copy = MessageBatchSentToAllWorkers;
            if (copy != null)
                copy(message);
        }

        private void HandleLoadBalancerMessage(OpenedQueue queue, Message message)
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
                    logger.DebugFormat("{0} is ready to work", work.Endpoint);
                    var needToAddToQueue = KnownWorkers.Add(work.Endpoint);

                    if (needToAddToQueue)
                        AddWorkerToQueue(queue,work);

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

        private void AddWorkerToQueue(OpenedQueue queue, ReadyToWork work)
        {
            var persistedWorker = new Message
            {
                Formatter = new XmlMessageFormatter(new[] { typeof(string) }),
                Body = work.Endpoint.ToString(),
                Label = ("Known worker: " + work.Endpoint).EnsureLabelLength()
            };
            logger.DebugFormat("New worker: {0}", work.Endpoint);
            queue.Send(persistedWorker.SetSubQueueToSendTo(SubQueue.Workers));

            SendToQueue(secondaryLoadBalancer, new NewWorkerPersisted
            {
                Endpoint = work.Endpoint
            });
            Raise(SentNewWorkerPersisted);
        }
    }
}
