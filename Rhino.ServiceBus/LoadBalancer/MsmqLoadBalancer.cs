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
using Rhino.ServiceBus.Transport;
using MessageType = Rhino.ServiceBus.Transport.MessageType;
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
		private MsmqReadyForWorkListener _readyForWorkListener;
		public event Action<Message> MessageBatchSentToAllWorkers;
		public event Action SentNewWorkerPersisted;
		public event Action SentNewEndpointPersisted;

		public MsmqLoadBalancer(
			IMessageSerializer serializer,
			IQueueStrategy queueStrategy,
			IEndpointRouter endpointRouter,
			Uri endpoint,
			int threadCount,
			TransactionalOptions transactional,
            IMessageBuilder<Message> messageBuilder)
			: base(queueStrategy, endpoint, threadCount, serializer, endpointRouter, transactional, messageBuilder)
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
					Uri secondaryLoadBalancer,
					TransactionalOptions transactional,
                    IMessageBuilder<Message> messageBuilder)
			: this(serializer, queueStrategy, endpointRouter, endpoint, threadCount, transactional, messageBuilder)
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
			using (var q = MsmqUtil.GetQueuePath(Endpoint).Open(QueueAccessMode.Receive))
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
							HandleLoadBalancerMessage(readyForWorkQueue, current);
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
			if (_readyForWorkListener != null)
			{
				_readyForWorkListener.ReadyToWorkMessageArrived += readyForWorkMessage => HandleReadyForWork(queue, readyForWorkMessage);
				_readyForWorkListener.Start();
			}
			
			if (secondaryLoadBalancer != null)
			{
				foreach (var queueUri in KnownEndpoints.GetValues())
				{
					logger.InfoFormat("Notifying {0} that primary load balancer {1} is taking over from secondary",
						queueUri,
						Endpoint.Uri
						);
					SendToQueue(queueUri, new Reroute
					{
						NewEndPoint = Endpoint.Uri,
						OriginalEndPoint = Endpoint.Uri
					});
				}

				SendHeartBeatToSecondaryServer(null);
				heartBeatTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
				Reroute reroute;
				if (_readyForWorkListener != null)
					reroute = new Reroute
					{
						NewEndPoint = _readyForWorkListener.Endpoint.Uri,
						OriginalEndPoint = _readyForWorkListener.Endpoint.Uri
					};
				else
					reroute = new Reroute
					{
						NewEndPoint = Endpoint.Uri,
						OriginalEndPoint = Endpoint.Uri
					};

				SendToAllWorkers(
					GenerateMsmqMessageFromMessageBatch(reroute),
					"Rerouting {1} back to {0}"
					);
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
		public MsmqReadyForWorkListener ReadyForWorkListener
		{
			get { return _readyForWorkListener; }
			set { _readyForWorkListener = value; }
		}
		
		protected void NotifyWorkersThatLoaderIsReadyToAcceptWork()
		{
			var acceptingWork = new AcceptingWork { Endpoint = Endpoint.Uri };
			SendToAllWorkers(
				GenerateMsmqMessageFromMessageBatch(acceptingWork),
				"Notifing {1} that {0} is accepting work"
				);
		}

		protected override void OnStop()
		{
			if(_readyForWorkListener !=null)
				_readyForWorkListener.Dispose();
			heartBeatTimer.Dispose();
		}
		
		protected override void HandlePeekedMessage(OpenedQueue queue, Message message)
		{
			try
			{
				using (var tx = new TransactionScope(TransactionScopeOption.Required, TransportUtil.GetTransactionTimeout()))
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
							HandleLoadBalancerMessage(queue, message);
							break;
						case MessageType.AdministrativeMessageMarker:
							SendToAllWorkers(message, "Dispatching administrative message from {0} to load balancer {1}");
							break;
						default:
							HandleStandardMessage(queue, message);
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

			logger.InfoFormat("Adding new endpoint: {0}", queueUri);
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
				var queryReadyForWorkQueueUri = msg as QueryReadyForWorkQueueUri;
				if (queryReadyForWorkQueueUri != null)
				{
					SendReadyForWorkQueueUri(message.ResponseQueue);
					continue;
				}
				var work = msg as ReadyToWork;
				if (work != null)
				{
					HandleReadyForWork(queue, work);
				}

				HandleLoadBalancerMessages(msg);
			}
		}

		
		private void HandleReadyForWork(OpenedQueue queue, ReadyToWork work)
		{
			logger.DebugFormat("{0} is ready to work", work.Endpoint);
			var needToAddToQueue = KnownWorkers.Add(work.Endpoint);

			if (needToAddToQueue)
				AddWorkerToQueue(queue, work);

			readyForWork.Enqueue(work.Endpoint);
		}

		private void SendReadyForWorkQueueUri(MessageQueue responseQueue)
		{
			if (responseQueue == null)
				return;
			try
			{
				var transactionType = MessageQueueTransactionType.None;
				if (Endpoint.Transactional.GetValueOrDefault())
					transactionType = Transaction.Current == null ? MessageQueueTransactionType.Single : MessageQueueTransactionType.Automatic;
				
				var newEndpoint = ReadyForWorkListener != null ? ReadyForWorkListener.Endpoint.Uri : Endpoint.Uri;
				var message = new ReadyForWorkQueueUri {Endpoint = newEndpoint};
				responseQueue.Send(GenerateMsmqMessageFromMessageBatch(message), transactionType);
			}
			catch (Exception e)
			{
				logger.Error("Failed to send known ready for work queue uri", e);
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

				var transactionType = MessageQueueTransactionType.None;
				if (Endpoint.Transactional.GetValueOrDefault())
					transactionType = Transaction.Current == null ? MessageQueueTransactionType.Single : MessageQueueTransactionType.Automatic;
				
				var index = 0;
				while (index < endpoints.Length)
				{
					var endpointsBatch = endpoints
						.Skip(index)
						.Take(256)
						.Select(x => new NewEndpointPersisted { PersistedEndpoint = x })
						.ToArray();
					index += endpointsBatch.Length;
					responseQueue.Send(GenerateMsmqMessageFromMessageBatch(endpointsBatch), transactionType);
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
					responseQueue.Send(GenerateMsmqMessageFromMessageBatch(workersBatch), transactionType);
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
