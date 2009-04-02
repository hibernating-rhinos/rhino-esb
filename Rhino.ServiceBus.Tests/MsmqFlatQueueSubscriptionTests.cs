using System;
using System.Linq;
using System.Messaging;
using Castle.MicroKernel;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Serializers;
using Rhino.ServiceBus.Transport;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class MsmqFlatQueueSubscriptionTests : MsmqFlatQueueTestBase
    {
        [Fact]
        public void Can_read_subscription_from_queue()
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(),new DefaultKernel());

            var msg = new Message();
            serializer.Serialize(new object[]{new AddSubscription
                                                  {
                                                      Endpoint = transactionalTestQueueEndpoint,
                                                      Type = typeof(TestMessage).FullName,
                                                  }}, msg.BodyStream);
            msg.Extension = Guid.NewGuid().ToByteArray();
            queue.OpenSiblngQueue(SubQueue.Subscriptions, QueueAccessMode.Send).Send(msg);


            var subscriptionStorage = new MsmqSubscriptionStorage(new DefaultReflection(),
                                                                  serializer,
                                                                  testQueueEndPoint.Uri,
                                                                  new EndpointRouter(),
                                                                  new FlatQueueStrategy(new EndpointRouter(),testQueueEndPoint.Uri));
            subscriptionStorage.Initialize();

            var uri = subscriptionStorage
                .GetSubscriptionsFor(typeof(TestMessage))
                .Single();

            Assert.Equal(transactionalTestQueueEndpoint.Uri, uri);
        }

        [Fact]
        public void Adding_then_removing_will_result_in_no_subscriptions()
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel());
            var msg = new Message();
            serializer.Serialize(new object[]{new AddSubscription
                                                  {
                                                      Endpoint = transactionalTestQueueEndpoint,
                                                      Type = typeof(TestMessage).FullName,
                                                  }}, msg.BodyStream);

            msg.Extension = Guid.NewGuid().ToByteArray();
            queue.OpenSiblngQueue(SubQueue.Subscriptions, QueueAccessMode.Send).Send(msg);

            var subscriptionStorage = new MsmqSubscriptionStorage(new DefaultReflection(),
                                                                  serializer,
                                                                  testQueueEndPoint.Uri,
                                                                  new EndpointRouter(),
                                                                  new FlatQueueStrategy(new EndpointRouter(),testQueueEndPoint.Uri));
            subscriptionStorage.Initialize();
            subscriptionStorage.RemoveSubscription(typeof(TestMessage).FullName, transactionalTestQueueEndpoint.Uri.ToString());

            var uris = subscriptionStorage
                .GetSubscriptionsFor(typeof(TestMessage));

            Assert.Equal(0, uris.Count());
        }

        public class TestMessage { }
    }
}
