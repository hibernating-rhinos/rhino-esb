using System;
using System.Messaging;
using Rhino.ServiceBus;
using Rhino.ServiceBus.Msmq;

namespace Starbucks
{
    public static class PrepareQueues
    {
        public static void Prepare(string queueName)
        {
            var queueUri = new Uri(queueName);
            var queuePath = MsmqUtil.GetQueuePath(new Endpoint
            {
                Uri = queueUri
            });
            CreateQueueIfNotExists(queuePath.QueuePath);
            PurgeQueue(queuePath.QueuePath);
            PurgeSubqueues(queuePath.QueuePath);
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

        private static void PurgeSubqueues(string queuePath)
        {
            PurgeSubqueue(queuePath, "errors");
            PurgeSubqueue(queuePath, "discarded");
            PurgeSubqueue(queuePath, "timeout");
            PurgeSubqueue(queuePath, "subscriptions");
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
