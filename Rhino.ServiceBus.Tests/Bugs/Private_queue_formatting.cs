using System.Messaging;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class Private_queue_formatting
    {
        [Fact]
        public void Will_be_translated_correctly()
        {
            const string privateQueue = @"FormatName:PRIVATE=0786bae3-b81d-48a4-9f84-413800f09f08\00000010";
            var uri = MsmqUtil.GetQueueUri(new MessageQueue(privateQueue));
            Assert.Equal("msmq://0786bae3-b81d-48a4-9f84-413800f09f08/00000010",
                uri.ToString());
            Assert.Equal(privateQueue,
                MsmqUtil.GetQueuePath(new Endpoint { Uri = uri }).QueuePath);
        }
    }
}