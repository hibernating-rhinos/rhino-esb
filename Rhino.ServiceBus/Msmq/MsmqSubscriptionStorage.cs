using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using log4net;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.Msmq
{
    public class MsmqSubscriptionStorage : ISubscriptionStorage, IMessageModule
    {
        private readonly Uri subscriptionQueue;
        private readonly Hashtable<string, List<WeakReference>> localInstanceSubscriptions = new Hashtable<string, List<WeakReference>>();
        private readonly MultiValueIndexHashtable<Guid, string, Uri, string> remoteInstanceSubscriptions = new MultiValueIndexHashtable<Guid, string, Uri, string>();

        private readonly Hashtable<string, HashSet<Uri>> subscriptions = new Hashtable<string, HashSet<Uri>>();
        private readonly Hashtable<TypeAndUriKey, IList<string>> subscriptionMessageIds = new Hashtable<TypeAndUriKey, IList<string>>();

        private readonly IReflection reflection;
        private readonly IMessageSerializer messageSerializer;
        private readonly IEndpointRouter endpointRouter;
        private readonly ILog logger = LogManager.GetLogger(typeof(MsmqSubscriptionStorage));
        private readonly IQueueStrategy queueStrategy;

        public MsmqSubscriptionStorage(
                    IReflection reflection,
                    IMessageSerializer messageSerializer,
                    Uri queueBusListensTo,
                    IEndpointRouter  endpointRouter,
                    IQueueStrategy queueStrategy
            )
        {
            this.reflection = reflection;
            this.messageSerializer = messageSerializer;
            this.endpointRouter = endpointRouter;
            this.queueStrategy = queueStrategy;
            this.subscriptionQueue = this.queueStrategy.CreateSubscriptionQueueUri(queueBusListensTo);
        }

        public void Initialize()
        {
            logger.DebugFormat("Initializing msmq subscription storage on: {0}", subscriptionQueue);
            using (var queue = CreateSubscriptionQueue(subscriptionQueue, QueueAccessMode.Receive))
            using (var enumerator = queue.GetMessageEnumerator2())
            {
                while (enumerator.MoveNext(TimeSpan.FromMilliseconds(0)))
                {
                    var current = enumerator.Current;
                    if (current == null)
                        continue;
                    object[] msgs;
                    try
                    {
                        msgs = messageSerializer.Deserialize(current.BodyStream);
                    }
                    catch (Exception e)
                    {
                        throw new SubscriptionException("Could not deserialize message from subscription queue", e);
                    }

                    try
                    {
                        foreach (var msg in msgs)
                        {
                            HandleAdministrativeMessage(new CurrentMessageInformation
                            {
                                AllMessages = msgs,
                                Message = msg,
                                TransportMessageId = current.Id,
                                MessageId = current.GetMessageId(),
                                Source = subscriptionQueue,
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        throw new SubscriptionException("Failed to process subscription records", e);
                    }
                }
            }
        }

        private void AddMessageIdentifierForTracking(string messageId, string messageType, Uri uri)
        {
            subscriptionMessageIds.Write(writer =>
            {
                var key = new TypeAndUriKey { TypeName = messageType, Uri = uri };
                IList<string> value;
                if (writer.TryGetValue(key, out value) == false)
                {
                    value = new List<string>();
                    writer.Add(key, value);
                }
                
                if(string.IsNullOrEmpty(messageId))
                    throw new ArgumentException("messageId must have value");

                value.Add(messageId);
            });
        }

        private void RemoveSubscriptionMessageFromQueue(OpenedQueue queue, string type, Uri uri)
        {
            subscriptionMessageIds.Write(writer =>
            {
                 var key = new TypeAndUriKey
                 {
                     TypeName = type,
                     Uri = uri
                 };
                 IList<string> messageIds;
                 if (writer.TryGetValue(key, out messageIds) == false)
                     return;
                 foreach (var msgId in messageIds)
                 {
                     queue.ConsumeMessage(msgId);
                 }
                 writer.Remove(key);
             });

        }

        private OpenedQueue CreateSubscriptionQueue(Uri subscriptionQueueUri, QueueAccessMode accessMode)
        {
        	OpenedQueue queue;
            try
            {
            	var endpoint = endpointRouter.GetRoutedEndpoint(subscriptionQueueUri);
				queue = MsmqUtil.GetQueuePath(endpoint).Open(accessMode, new XmlMessageFormatter(new[] { typeof(string) }));
            }
            catch (Exception e)
            {
                throw new SubscriptionException("Could not open subscription queue (" + subscriptionQueueUri + ")", e);
            }
            return queue;
        }

        public IEnumerable<Uri> GetSubscriptionsFor(Type type)
        {
            HashSet<Uri> subscriptionForType = null;
            subscriptions.Read(reader => reader.TryGetValue(type.FullName, out subscriptionForType));
            var subscriptionsFor = subscriptionForType ?? new HashSet<Uri>();

            List<Uri> instanceSubscriptions;
            remoteInstanceSubscriptions.TryGet(type.FullName, out instanceSubscriptions);

            subscriptionsFor.UnionWith(instanceSubscriptions);

            return subscriptionsFor;
        }

        public void RemoveLocalInstanceSubscription(IMessageConsumer consumer)
        {
            var messagesConsumes = reflection.GetMessagesConsumed(consumer);
            bool changed = false;
            var list = new List<WeakReference>();

            localInstanceSubscriptions.Write(writer =>
            {
                foreach (var type in messagesConsumes)
                {
                    List<WeakReference> value;

                    if (writer.TryGetValue(type.FullName, out value) == false)
                        continue;
                    writer.Remove(type.FullName);
                    list.AddRange(value);
                }
            });

            foreach (WeakReference reference in list)
            {
                if (ReferenceEquals(reference.Target, consumer))
                    continue;

                changed = true;

            }
            if (changed)
                RaiseSubscriptionChanged();
        }

        public object[] GetInstanceSubscriptions(Type type)
        {
            List<WeakReference> value = null;

            localInstanceSubscriptions.Read(reader => reader.TryGetValue(type.FullName, out value));

            if (value == null)
                return new object[0];

            var liveInstances = value
                .Select(x => x.Target)
                .Where(x => x != null)
                .ToArray();

            if (liveInstances.Length != value.Count)//cleanup
            {
                localInstanceSubscriptions.Write(writer => value.RemoveAll(x => x.IsAlive == false));
            }

            return liveInstances;
        }

        public bool HandleAdministrativeMessage(CurrentMessageInformation msgInfo)
        {
            var addSubscription = msgInfo.Message as AddSubscription;
            if (addSubscription != null)
            {
                return ConsumeAddSubscription(msgInfo, addSubscription);
            }
            var removeSubscription = msgInfo.Message as RemoveSubscription;
            if (removeSubscription != null)
            {
                return ConsumeRemoveSubscription(removeSubscription);
            }
            var addInstanceSubscription = msgInfo.Message as AddInstanceSubscription;
            if (addInstanceSubscription != null)
            {
                return ConsumeAddInstanceSubscription(msgInfo, addInstanceSubscription);
            }
            var removeInstanceSubscription = msgInfo.Message as RemoveInstanceSubscription;
            if (removeInstanceSubscription != null)
            {
                return ConsumeRemoveInstanceSubscrion(removeInstanceSubscription);
            }
            return false;
        }

        private bool ConsumeRemoveInstanceSubscrion(RemoveInstanceSubscription subscription)
        {
            string msgId;
            if(remoteInstanceSubscriptions.TryRemove(subscription.InstanceSubscriptionKey,out msgId))
            {
                using (var queue = CreateSubscriptionQueue(subscriptionQueue, QueueAccessMode.Receive))
                {
                    queue.ConsumeMessage(msgId);
                }
                RaiseSubscriptionChanged();
            }
            return true;
        }

        private bool ConsumeAddInstanceSubscription(CurrentMessageInformation msgInfo, AddInstanceSubscription subscription)
        {
            remoteInstanceSubscriptions.Add(
                subscription.InstanceSubscriptionKey, 
                subscription.Type,
                new Uri(subscription.Endpoint), 
                msgInfo.TransportMessageId);
            var msmqMsgInfo = msgInfo as MsmqCurrentMessageInformation;
            if (msmqMsgInfo != null)
            {
                msmqMsgInfo.Queue.Send(
                    msmqMsgInfo.MsmqMessage.SetSubQueueToSendTo(SubQueue.Subscriptions));
            }
            RaiseSubscriptionChanged();
            return true;
        }

        private bool ConsumeRemoveSubscription(RemoveSubscription removeSubscription)
        {
            RemoveSubscription(removeSubscription.Type, removeSubscription.Endpoint);
            return true;
        }

        private bool ConsumeAddSubscription(CurrentMessageInformation msgInfo, AddSubscription addSubscription)
        {
            bool newSubscription = AddSubscription(addSubscription.Type, addSubscription.Endpoint);

            
            var msmqMsgInfo = msgInfo as MsmqCurrentMessageInformation;

            if (msmqMsgInfo != null && newSubscription)
            {
                Message message = msmqMsgInfo.MsmqMessage;
                msmqMsgInfo.Queue.Send(
                   message.SetSubQueueToSendTo(SubQueue.Subscriptions));
            
                AddMessageIdentifierForTracking(
                    message.Id,
                    addSubscription.Type,
                    new Uri(addSubscription.Endpoint));
            
                return true;
            }
            
            AddMessageIdentifierForTracking(
                msgInfo.TransportMessageId,
                addSubscription.Type,
                new Uri(addSubscription.Endpoint));
            return false;
        }


        public event Action SubscriptionChanged;

        public bool AddSubscription(string type, string endpoint)
        {
            bool added = false;
            subscriptions.Write(writer =>
            {
                HashSet<Uri> subscriptionsForType;
                if (writer.TryGetValue(type, out subscriptionsForType) == false)
                {
                    subscriptionsForType = new HashSet<Uri>();
                    writer.Add(type, subscriptionsForType);
                }

                var uri = new Uri(endpoint);
                added = subscriptionsForType.Add(uri);

                logger.InfoFormat("Added subscription for {0} on {1}",
                                  type, uri);
            });

            RaiseSubscriptionChanged();
            return added;
        }

        private void RaiseSubscriptionChanged()
        {
            var copy = SubscriptionChanged;
            if (copy != null)
                copy();
        }

        public void RemoveSubscription(string type, string endpoint)
        {
            var uri = new Uri(endpoint);
            using (var queue = CreateSubscriptionQueue(subscriptionQueue, QueueAccessMode.Receive))
            {
                RemoveSubscriptionMessageFromQueue(queue, type, uri);
            }

            subscriptions.Write(writer =>
            {
                HashSet<Uri> subscriptionsForType;

                if (writer.TryGetValue(type, out subscriptionsForType) == false)
                {
                    subscriptionsForType = new HashSet<Uri>();
                    writer.Add(type, subscriptionsForType);
                }

                subscriptionsForType.Remove(uri);

                logger.InfoFormat("Removed subscription for {0} on {1}",
                                  type, endpoint);
            });

            RaiseSubscriptionChanged();
        }

        public void AddLocalInstanceSubscription(IMessageConsumer consumer)
        {
            localInstanceSubscriptions.Write(writer =>
            {
                foreach (var type in reflection.GetMessagesConsumed(consumer))
                {
                    List<WeakReference> value;
                    if (writer.TryGetValue(type.FullName, out value) == false)
                    {
                        value = new List<WeakReference>();
                        writer.Add(type.FullName, value);
                    }
                    value.Add(new WeakReference(consumer));
                }
            });
            RaiseSubscriptionChanged();
        }

        void IMessageModule.Init(ITransport transport)
        {
            transport.AdministrativeMessageArrived += HandleAdministrativeMessage;
        }

        void IMessageModule.Stop(ITransport transport)
        {
            transport.AdministrativeMessageArrived -= HandleAdministrativeMessage;
        }
    }
}
