using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Rhino.PersistentHashTable;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Transport;

namespace Rhino.ServiceBus.RhinoQueues
{
    public class PhtSubscriptionStorage : ISubscriptionStorage, IDisposable, IMessageModule
    {
        private const string subscriptionsKey = "subscriptions";

        private readonly Hashtable<string, List<WeakReference>> localInstanceSubscriptions =
            new Hashtable<string, List<WeakReference>>();

        private readonly ILog logger = LogManager.GetLogger(typeof (PhtSubscriptionStorage));

        private readonly IMessageSerializer messageSerializer;
        private readonly PersistentHashTable.PersistentHashTable pht;
        private readonly IReflection reflection;

        private readonly MultiValueIndexHashtable<Guid, string, Uri, int> remoteInstanceSubscriptions =
            new MultiValueIndexHashtable<Guid, string, Uri, int>();

        private readonly Hashtable<TypeAndUriKey, IList<int>> subscriptionMessageIds =
            new Hashtable<TypeAndUriKey, IList<int>>();

        private readonly string subscriptionPath;
        private readonly Hashtable<string, HashSet<Uri>> subscriptions = new Hashtable<string, HashSet<Uri>>();

        public PhtSubscriptionStorage(
            string subscriptionPath, 
            IMessageSerializer messageSerializer,
            IReflection reflection)
        {
            this.subscriptionPath = subscriptionPath;
            this.messageSerializer = messageSerializer;
            this.reflection = reflection;
            pht = new PersistentHashTable.PersistentHashTable(subscriptionPath);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (pht != null)
                pht.Dispose();
        }

        #endregion

        #region IMessageModule Members

        void IMessageModule.Init(ITransport transport)
        {
            transport.AdministrativeMessageArrived += HandleAdministrativeMessage;
        }

        void IMessageModule.Stop(ITransport transport)
        {
            transport.AdministrativeMessageArrived -= HandleAdministrativeMessage;
        }

        #endregion

        #region ISubscriptionStorage Members

        public void Initialize()
        {
            logger.DebugFormat("Initializing msmq subscription storage on: {0}", subscriptionPath);
            pht.Initialize();

            pht.Batch(actions =>
            {
                var items = actions.GetItems(new GetItemsRequest
                {
                    Key = subscriptionsKey
                });
                foreach (var item in items)
                {
                    object[] msgs;
                    try
                    {
                        msgs = messageSerializer.Deserialize(new MemoryStream(item.Value));
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
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        throw new SubscriptionException("Failed to process subscription records", e);
                    }
                }
                actions.Commit();
            });
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
            var changed = false;
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

            foreach (var reference in list)
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

            if (liveInstances.Length != value.Count) //cleanup
            {
                localInstanceSubscriptions.Write(writer => value.RemoveAll(x => x.IsAlive == false));
            }

            return liveInstances;
        }

        public event Action SubscriptionChanged;

        public bool AddSubscription(string type, string endpoint)
        {
            var added = false;
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

        public void RemoveSubscription(string type, string endpoint)
        {
            var uri = new Uri(endpoint);
            RemoveSubscriptionMessageFromPht(type, uri);

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

        #endregion

        private void AddMessageIdentifierForTracking(int messageId, string messageType, Uri uri)
        {
            subscriptionMessageIds.Write(writer =>
            {
                var key = new TypeAndUriKey {TypeName = messageType, Uri = uri};
                IList<int> value;
                if (writer.TryGetValue(key, out value) == false)
                {
                    value = new List<int>();
                    writer.Add(key, value);
                }

                value.Add(messageId);
            });
        }

        private void RemoveSubscriptionMessageFromPht(string type, Uri uri)
        {
            subscriptionMessageIds.Write(writer =>
            {
                var key = new TypeAndUriKey
                {
                    TypeName = type,
                    Uri = uri
                };
                IList<int> messageIds;
                if (writer.TryGetValue(key, out messageIds) == false)
                    return;

                pht.Batch(actions =>
                {
                    foreach (var msgId in messageIds)
                    {
                        actions.RemoveItem(new RemoveItemRequest
                        {
                            Id = msgId,
                            Key = subscriptionsKey
                        });
                    }

                    actions.Commit();
                });
                writer.Remove(key);
            });
        }

        public bool HandleAdministrativeMessage(CurrentMessageInformation msgInfo)
        {
            var addSubscription = msgInfo.Message as AddSubscription;
            if (addSubscription != null)
            {
                return ConsumeAddSubscription(addSubscription);
            }
            var removeSubscription = msgInfo.Message as RemoveSubscription;
            if (removeSubscription != null)
            {
                return ConsumeRemoveSubscription(removeSubscription);
            }
            var addInstanceSubscription = msgInfo.Message as AddInstanceSubscription;
            if (addInstanceSubscription != null)
            {
                return ConsumeAddInstanceSubscription(addInstanceSubscription);
            }
            var removeInstanceSubscription = msgInfo.Message as RemoveInstanceSubscription;
            if (removeInstanceSubscription != null)
            {
                return ConsumeRemoveInstanceSubscription(removeInstanceSubscription);
            }
            return false;
        }

        public bool ConsumeRemoveInstanceSubscription(RemoveInstanceSubscription subscription)
        {
            int msgId;
            if (remoteInstanceSubscriptions.TryRemove(subscription.InstanceSubscriptionKey, out msgId))
            {
                pht.Batch(actions =>
                {
                    actions.RemoveItem(new RemoveItemRequest
                    {
                        Id = msgId,
                        Key = subscriptionsKey
                    });

                    actions.Commit();
                });
                RaiseSubscriptionChanged();
            }
            return true;
        }

        public bool ConsumeAddInstanceSubscription(
            AddInstanceSubscription subscription)
        {
            pht.Batch(actions =>
            {
                var message = new MemoryStream();
                messageSerializer.Serialize(new[] {subscription}, message);
                var itemId = actions.AddItem(new AddItemRequest
                {
                    Key = subscriptionsKey,
                    Data = message.ToArray()
                });

                remoteInstanceSubscriptions.Add(
                    subscription.InstanceSubscriptionKey,
                    subscription.Type,
                    new Uri(subscription.Endpoint),
                    itemId);

                actions.Commit();
            });

            RaiseSubscriptionChanged();
            return true;
        }

        public bool ConsumeRemoveSubscription(RemoveSubscription removeSubscription)
        {
            RemoveSubscription(removeSubscription.Type, removeSubscription.Endpoint.Uri.ToString());
            return true;
        }

        public bool ConsumeAddSubscription(AddSubscription addSubscription)
        {
            var newSubscription = AddSubscription(addSubscription.Type, addSubscription.Endpoint.Uri.ToString());


            if (newSubscription)
            {
                var itemId = 0;
                pht.Batch(actions =>
                {
                    var stream = new MemoryStream();
                    messageSerializer.Serialize(new[] {addSubscription}, stream);
                    itemId = actions.AddItem(new AddItemRequest
                    {
                        Key = subscriptionsKey,
                        Data = stream.ToArray()
                    });

                    actions.Commit();
                });

                AddMessageIdentifierForTracking(
                    itemId,
                    addSubscription.Type,
                    addSubscription.Endpoint.Uri);


                return true;
            }

            return false;
        }


        private void RaiseSubscriptionChanged()
        {
            var copy = SubscriptionChanged;
            if (copy != null)
                copy();
        }
    }
}