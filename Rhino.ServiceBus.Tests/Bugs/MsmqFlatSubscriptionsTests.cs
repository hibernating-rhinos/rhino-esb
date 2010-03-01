using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class MsmqFlatSubscriptionsTests : MsmqFlatQueueTestBase
    {
        [Fact]
        public void Adding_then_removing_instance_will_result_in_no_subscriptions()
        {
            var strategy = new FlatQueueStrategy(new EndpointRouter(),
                                                 testQueueEndPoint.Uri);
            SubscriptionTest.Test(Transport, strategy,
                                  testQueueEndPoint, queue.Send, subscriptions.GetMessageEnumerator2);
        }
    }
}