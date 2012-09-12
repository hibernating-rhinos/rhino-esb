using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using System.Xml;
using Common.Logging;
using Rhino.Queues;
using Rhino.Queues.Model;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Transport;
using Message=Rhino.Queues.Model.Message;

namespace Rhino.ServiceBus.RhinoQueues
{
    public class TimeoutAction : IDisposable
    {
        private readonly IQueue queue;
        private readonly ILog logger = LogManager.GetLogger(typeof (TimeoutAction));
        private readonly Timer timeoutTimer;
        private readonly OrderedList<DateTime, MessageId> timeoutMessageIds =
            new OrderedList<DateTime, MessageId>();

        [CLSCompliant(false)]
        public TimeoutAction(IQueue queue)
        {
            this.queue = queue;
            timeoutMessageIds.Write(writer =>
            {
                foreach (var message in queue.GetAllMessages(SubQueue.Timeout.ToString()))
                {
                    var timeToSend = XmlConvert.ToDateTime(message.Headers["time-to-send"],XmlDateTimeSerializationMode.Utc);
                    logger.DebugFormat("Registering message {0} to be sent at {1} on {2}",
                                  message.Id, timeToSend, queue.QueueName);
                    writer.Add(timeToSend, message.Id);
                }
            });
            timeoutTimer = new Timer(OnTimeoutCallback, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
        }

        public static DateTime CurrentTime
        {
            get { return DateTime.Now; }
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
                KeyValuePair<DateTime, List<MessageId>> pair;
                while (writer.TryRemoveFirstUntil(CurrentTime, out pair))
                {
                    if (pair.Key > CurrentTime)
                        return;
                    foreach (var messageId in pair.Value)
                    {
                        try
                        {
                            logger.DebugFormat("Moving message {0} to main queue: {1}",
                                               messageId, queue.QueueName);
                            using (var tx = new TransactionScope())
                            {
                                var message = queue.PeekById(messageId);
                                if (message == null)
                                    continue;
                                queue.MoveTo(null, message);
                                tx.Complete();
                            }
                        }
                        catch (Exception)
                        {
                            logger.DebugFormat(
                                "Could not move message {0} to main queue: {1}",
                                messageId,
                                queue.QueueName);

                            if ((CurrentTime - pair.Key).TotalMinutes >= 1.0D)
                            {
                                logger.DebugFormat("Tried to send message {0} for over a minute, giving up",
                                                   messageId);
                                continue;
                            }

                            writer.Add(pair.Key, messageId);
                            logger.DebugFormat("Will retry moving message {0} to main queue {1} in 1 second",
                                               messageId,
                                               queue.QueueName);
                        } 
                    }
                } 
            });
        }

        public void Dispose()
        {
            if (timeoutTimer != null)
                timeoutTimer.Dispose();
        }

        [CLSCompliant(false)]
        public void Register(Message message)
        {
            timeoutMessageIds.Write(writer =>
            {
                var timeToSend = XmlConvert.ToDateTime(message.Headers["time-to-send"], XmlDateTimeSerializationMode.Utc);

                logger.DebugFormat("Registering message {0} to be sent at {1} on {2}",
                                   message.Id, timeToSend, queue.QueueName);

                writer.Add(timeToSend, message.Id);    
            });
        }
    }
}