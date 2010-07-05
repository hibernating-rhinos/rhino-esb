using System.Messaging;

namespace Rhino.ServiceBus
{
    public interface IMessageBuilder
    {
        Message GenerateMsmqMessageFromMessageBatch(params object[] msgs);
    }
}