using System;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Threading;
using System.Transactions;
using log4net;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.Msmq
{
    public abstract class AbstractMsmqListener : IDisposable
    {
        private readonly IQueueStrategy queueStrategy;
        private readonly Uri endpoint;
        private readonly Thread[] threads;
        private bool haveStarted;

        private volatile bool shouldStop;

        private readonly IMessageSerializer messageSerializer;
        private readonly ILog logger = LogManager.GetLogger(typeof(AbstractMsmqListener));

        private readonly int threadCount;

        public event Action MessageMoved;
        public event Action TransportMessageArrived;


        protected AbstractMsmqListener(
            IQueueStrategy queueStrategy,
            Uri endpoint,
            int threadCount,
            IMessageSerializer messageSerializer,
            IEndpointRouter endpointRouter)
        {
            this.queueStrategy = queueStrategy;
            this.messageSerializer = messageSerializer;
            this.endpointRouter = endpointRouter;
            this.endpoint = endpoint;
            this.threadCount = threadCount;
            threads = new Thread[threadCount];
        }

        public event Action Started;

        public bool HaveStarted
        {
            get { return haveStarted; }
        }

        public int ThreadCount
        {
            get { return threadCount; }
        }

        public Endpoint Endpoint
        {
            get
            {
                return endpointRouter.GetRoutedEndpoint(endpoint);
            }
        }

        protected static TimeSpan TimeOutForPeek
        {
            get { return TimeSpan.FromHours(1); }
        }

        public void Start()
        {
            if (haveStarted)
                return;
            logger.DebugFormat("Starting msmq transport on: {0}", Endpoint);
            using (var queue = InitalizeQueue(Endpoint))
            {
                BeforeStart(queue);

                shouldStop = false;
                TransportState = TransportState.Started;

                for (var t = 0; t < ThreadCount; t++)
                {
                    var thread = new Thread(PeekMessageOnBackgroundThread)
                    {
                        Name = "Rhino Service Bus Worker Thread #" + t,
                        IsBackground = true
                    };
                    threads[t] = thread;
                    thread.Start();
                }

                haveStarted = true;

                var copy = Started;
                if (copy != null)
                    copy();

                AfterStart(queue);
            }
        }

        protected virtual void AfterStart(OpenedQueue queue)
        {

        }

        protected virtual void BeforeStart(OpenedQueue queue)
        {

        }

        public void Dispose()
        {
            shouldStop = true;
            OnStop();
            using (var queue = InitalizeQueue(Endpoint))
            {
                queue.SendInSingleTransaction(new Message
                {
                    Label = "Shutdown bus",
                    Extension = Guid.NewGuid().ToByteArray(),
                    AppSpecific = (int)MessageType.ShutDownMessageMarker
                });
            }

            WaitForProcessingToEnd();

            haveStarted = false;
            TransportState = TransportState.Stopped;
        }

        protected virtual void OnStop()
        {

        }

        private void WaitForProcessingToEnd()
        {
            if (haveStarted == false)
                return;

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        protected static OpenedQueue InitalizeQueue(Endpoint endpoint)
        {
            try
            {
                return MsmqUtil.GetQueuePath(endpoint).Open(QueueAccessMode.SendAndReceive);
            }
            catch (Exception e)
            {
                throw new TransportException(
                    "Could not open queue: " + endpoint + Environment.NewLine +
                    "Queue path: " + MsmqUtil.GetQueuePath(endpoint) + Environment.NewLine +
                    "Did you forget to create the queue or disable the queue initialization module?", e);
            }

        }

        protected void PeekMessageOnBackgroundThread(object state)
        {
            using(var queue = InitalizeQueue(Endpoint))
            while (shouldStop == false)
            {
                try
                {
                    Message message;
                    bool? peek = TryPeek(queue,out message);

                    if (peek == false || shouldStop)//error reading from queue
                    {
                        TransportState = TransportState.FailedToReadFromQueue;
                        return; // return from method, we have failed}
                    }
                    if (peek == null) //nothing was found 
                        continue;

                    if ((MessageType)((message.AppSpecific & 0xFFFF0000) >> 16) == MessageType.MoveMessageMarker)
                    {
                        var subQueue = (SubQueue)(0x0000FFFF & message.AppSpecific);
                        using (var tx = new TransactionScope())
                        {
                            string msgId;
                            queueStrategy.TryMoveMessage(queue, message, subQueue, out msgId);
                            tx.Complete();
                        }
                        Raise(MessageMoved);
                        continue;
                    }
                    string responseQueue = "null://middle/of/nowhere?turn=left";
                    if (message.ResponseQueue!=null)
                        responseQueue = message.ResponseQueue.Path;
                    logger.DebugFormat("Got message {0} on {1} from {2}",
                                       message.Label,
                                       queue.RootUri,
                                       responseQueue);

                    Raise(TransportMessageArrived);

                    HandlePeekedMessage(queue, message);
                }
                catch (ThreadAbortException)
                {
                    //nothing much to do here, process is being killed
                    //or someone is trying to do something rude to us
                }
                catch (Exception e)
                {
#if DEBUG
                    Debugger.Break();
                    Debug.Fail("should not happen", e.ToString());
#endif
                    logger.Fatal("BUG_IN_THE_BUS: An error occured during message dispatch by the bus itself. Please notify the developers", e);
                }
            }

        }

        protected static void Raise(Action action)
        {
            var copy = action;
            if (copy != null)
                copy();
        }

        protected IEndpointRouter endpointRouter;

        public TransportState TransportState { get; set; }

        protected abstract void HandlePeekedMessage(OpenedQueue queue, Message message);


        private bool? TryPeek(OpenedQueue queue, out Message message)
        {
            try
            {
                message = queue.Peek(TimeOutForPeek);
            }
            catch (MessageQueueException e)
            {
                message = null;
                if (e.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                {
                    logger.Error("Could not peek message from queue", e);
                    return false;
                }
                return null; // nothing found
            }
            catch (Exception e)
            {
                message = null;
                logger.Error("Could not peek message from queue", e);
                return false;
            }
            return true;
        }

        protected static TimeSpan GetTransactionTimeout()
        {
            if (Debugger.IsAttached)
                return TimeSpan.FromMinutes(45);
            return TimeSpan.Zero;
        }

        protected Message GenerateMsmqMessageFromMessageBatch(params object[] msgs)
        {
            var message = new Message();

            messageSerializer.Serialize(msgs, message.BodyStream);

            message.ResponseQueue = InitalizeQueue(Endpoint).ToResponseQueue();

            message.Extension = Guid.NewGuid().ToByteArray();

            message.AppSpecific = GetAppSpecificMarker(msgs);

            message.Label = msgs
                .Where(msg => msg != null)
                .Select(msg =>
                {
                    string s = msg.ToString();
                    if (s.Length > 249)
                        return s.Substring(0, 246) + "...";
                    return s;
                })
                .FirstOrDefault();
            return message;
        }

        protected static int GetAppSpecificMarker(object[] msgs)
        {
            var msg = msgs[0];
            if (msg is AdministrativeMessage)
                return (int)MessageType.AdministrativeMessageMarker;
            if (msg is LoadBalancerMessage)
                return (int)MessageType.LoadBalancerMessageMarker;
            return 0;
        }

        protected object[] DeserializeMessages(OpenedQueue messageQueue, Message transportMessage, Action<CurrentMessageInformation, Exception> messageSerializationException)
        {
            try
            {
                return messageSerializer.Deserialize(transportMessage.BodyStream);
            }
            catch (Exception e)
            {
                try
                {
                    logger.Error("Error when serializing message", e);
                    if (messageSerializationException != null)
                    {
                        var information = new MsmqCurrentMessageInformation
                        {
                            MsmqMessage = transportMessage,
                            Queue = messageQueue,
                            Message = transportMessage,
                            Source = messageQueue.RootUri,
                            MessageId = transportMessage.GetMessageId()
                        };
                        messageSerializationException(information, e);
                    }
                }
                catch (Exception moduleEx)
                {
                    logger.Error("Error when notifying about serialization exception", moduleEx);
                }
                throw;
            }
        }
    }
}
