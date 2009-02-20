using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading;
using System.Transactions;
using log4net;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Msmq.TransportActions
{
    public class TimeoutAction : AbstractTransportAction, IDisposable
    {
        private readonly ILog logger = LogManager.GetLogger(typeof (TimeoutAction));
        private readonly IQueueStrategy queueStrategy;
        private Timer timeoutTimer;
        private IMsmqTransport parentTransport;
        private readonly OrderedList<DateTime, string> timeoutMessageIds =
            new OrderedList<DateTime, string>();

        public TimeoutAction(IQueueStrategy queueStrategy)
        {
            this.queueStrategy = queueStrategy;
        }

        public override void Init(IMsmqTransport transport, OpenedQueue queue)
        {
            parentTransport = transport;
            timeoutMessageIds.Write(writer =>
            {
                foreach (var message in queueStrategy.GetTimeoutMessages(queue))
                {
                    writer.Add(message.Time, message.Id);
                }
            });
            timeoutTimer = new Timer(OnTimeoutCallback, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
        }

        public override MessageType HandledType
        {
            get { return MessageType.TimeoutMessageMarker; }
        }

        public static DateTime CurrentTime
        {
            get { return DateTime.Now; }
        }

        public override bool HandlePeekedMessage(IMsmqTransport transport, OpenedQueue queue, Message message)
        {
          using(var tx = new TransactionScope())
          {
              var processMessageAt = DateTime.FromBinary(BitConverter.ToInt64(message.Extension, 16));
              if (CurrentTime >= processMessageAt)
                  return false;

              var msg = queue.TryGetMessageFromQueue(message.Id);
              if (msg == null)
                  return false;


              queue.Send(message.SetSubQueueToSendTo(SubQueue.Timeout));
              tx.Complete();

              logger.DebugFormat("Moving message {0} to timeout queue, will be processed at: {1}",
                                 message.Id,
                                 processMessageAt);

              timeoutMessageIds.Write(writer => writer.Add(processMessageAt, message.Id));

              return true;
          }
        }

        private void OnTimeoutCallback(object state)
        {
            bool haveTimeoutMessages = false;

            timeoutMessageIds.Read(reader =>
                  haveTimeoutMessages = reader.HasAnyBefore(CurrentTime)
                );

            if (haveTimeoutMessages == false)
                return;

            timeoutMessageIds.Write(writer =>
            {
                KeyValuePair<DateTime, string> pair;
                while (writer.TryRemoveFirstUntil(CurrentTime,out pair))
                {
                    if (pair.Key > CurrentTime)
                        return;
                    Uri queueUri = null;
                    try
                    {
                        using(var queue = parentTransport.CreateQueue())
                        using (var tx = new TransactionScope())
                        {
                            queueUri = queue.RootUri;
                            logger.DebugFormat("Moving message {0} to main queue: {1}",
                                           pair.Value,
                                           queueUri);
                            queueStrategy.MoveTimeoutToMainQueue(queue, pair.Value);
                            tx.Complete();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        logger.DebugFormat(
                            "Could not move message {0} to main queue: {1}",
                            pair.Value,
                            queueUri);

                        writer.Add(DateTime.Now.AddSeconds(1), pair.Value);
                        logger.DebugFormat("Will retry moving message {0} to main queue {1} in 1 second", 
                                pair.Value,
                                queueUri);
                    }
                } 
            });
        }

        public void Dispose()
        {
            if (timeoutTimer != null)
                timeoutTimer.Dispose();
        }
    }
}