using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading;
using System.Transactions;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Msmq.TransportActions
{
    public class TimeoutAction : AbstractTransportAction, IDisposable
    {
        private readonly IQueueStrategy queueStrategy;
        private Timer timeoutTimer;
        private IMsmqTransport parentTransport;
        private readonly OrderedList<DateTime, string> timeoutMessageIds =
            new OrderedList<DateTime, string>();

        public TimeoutAction(IQueueStrategy queueStrategy)
        {
            this.queueStrategy = queueStrategy;
        }

        public override void Init(IMsmqTransport transport)
        {
            parentTransport = transport;
            timeoutMessageIds.Write(writer =>
            {
                foreach (var message in queueStrategy.GetTimeoutMessages(transport.Queue))
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

                    try
                    {
                        using (var tx = new TransactionScope())
                        {
                            queueStrategy.MoveTimeoutToMainQueue(parentTransport.Queue, pair.Value);
                            tx.Complete();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        //message read by another thread
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