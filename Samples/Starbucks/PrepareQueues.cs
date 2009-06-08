using System;
using System.Messaging;
using Rhino.ServiceBus;
using Rhino.ServiceBus.Msmq;

namespace Starbucks
{
    public static class PrepareQueues
    {
        public static void Prepare(string queueName, QueueType queueType)
        {
            var queueUri = new Uri(queueName);
            var queuePath = MsmqUtil.GetQueuePath(new Endpoint
            {
                Uri = queueUri
            });
            CreateQueueIfNotExists(queuePath.QueuePath);
            PurgeQueue(queuePath.QueuePath);
            PurgeSubqueues(queuePath.QueuePath, queueType);
        }

        private static void CreateQueueIfNotExists(string queuePath)
        {
            if (!MessageQueue.Exists(queuePath))
            {
                MessageQueue.Create(queuePath);
            }
        }

        private static void PurgeQueue(string queuePath)
        {
            using (var queue = new MessageQueue(queuePath))
            {
                queue.Purge();
            }
        }

        private static void PurgeSubqueues(string queuePath, QueueType queueType)
        {
            switch (queueType)
            {
                case QueueType.Standard:
                    PurgeSubqueue(queuePath, "errors");
                    PurgeSubqueue(queuePath, "discarded");
                    PurgeSubqueue(queuePath, "timeout");
                    PurgeSubqueue(queuePath, "subscriptions");
                    break;
                case QueueType.LoadBalancer:
                    PurgeSubqueue(queuePath, "endpoints");
                    PurgeSubqueue(queuePath, "workers");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("queueType", "Can't handle queue type: " + queueType);
            }
        }

        private static void PurgeSubqueue(string queuePath, string subqueueName)
        {
            using (var queue = new MessageQueue(queuePath + ";" + subqueueName))
            {
                queue.Purge();
            }
        }
    }
}
