using System.Messaging;

namespace Rhino.ServiceBus.Msmq
{
    public interface IMessageBuilder
    {
        Message GenerateMsmqMessageFromMessageBatch(params object[] msgs);
    }
}