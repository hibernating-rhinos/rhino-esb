using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class MsmqSubQueueSubscriptionTests : MsmqTestBase
    {
        [Fact]
        public void Adding_then_removing_instance_will_result_in_no_subscriptions()
        {
            SubscriptionTest.Test(Transport, new SubQueueStrategy(),
                                  TestQueueUri, queue.Send, subscriptions.GetMessageEnumerator2);
        }
    }
}