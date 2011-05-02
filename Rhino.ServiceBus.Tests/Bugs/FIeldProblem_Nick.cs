using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Threading;
using Castle.MicroKernel;
using Castle.Windsor;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Serializers;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public static class SubscriptionTest
    {
        public static void Test(ITransport t, IQueueStrategy strategy,
                                Endpoint queueEndpoint, Action<Message> send, Func<MessageEnumerator>
                                                                                  enumer)
        {
            Guid id = Guid.NewGuid();
            var serializer = new XmlMessageSerializer(new DefaultReflection(),
                                                      new CastleServiceLocator(new WindsorContainer()));

            var subscriptionStorage = new MsmqSubscriptionStorage(new
                                                                      DefaultReflection(),
                                                                  serializer,
                                                                  queueEndpoint.Uri,
                                                                  new
                                                                      EndpointRouter(),
                                                                  strategy);
            subscriptionStorage.Initialize();

            var wait = new ManualResetEvent(false);

            subscriptionStorage.SubscriptionChanged += () => wait.Set();

            t.AdministrativeMessageArrived +=
                subscriptionStorage.HandleAdministrativeMessage;

            Message msg = new MsmqMessageBuilder
                (serializer, new CastleServiceLocator(new WindsorContainer())).BuildFromMessageBatch(new
                                                                     AddInstanceSubscription
                {
                    Endpoint = queueEndpoint.Uri.ToString(),
                    InstanceSubscriptionKey = id,
                    Type = typeof (TestMessage2).FullName,
                });
            send(msg);

            wait.WaitOne();

            msg = new MsmqMessageBuilder
                (serializer, new CastleServiceLocator(new WindsorContainer())).BuildFromMessageBatch(new
                                                                     RemoveInstanceSubscription
                {
                    Endpoint = queueEndpoint.Uri.ToString(),
                    InstanceSubscriptionKey = id,
                    Type = typeof (TestMessage2).FullName,
                });
           
            wait.Reset();

            send(msg);

            wait.WaitOne();

            IEnumerable<Uri> uris = subscriptionStorage
                .GetSubscriptionsFor(typeof (TestMessage2));
            Assert.Equal(0, uris.Count());

            int count = 0;
            MessageEnumerator copy = enumer();
            while (copy.MoveNext()) count++;
            Assert.Equal(0, count);
        }
    }

    public class TestMessage2
    {
    }
}