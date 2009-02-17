using System.IO;

namespace Rhino.ServiceBus.Internal
{
    public interface IMessageSerializer
    {
        void Serialize(object[] messages, Stream message);

        object[] Deserialize(Stream message);
    }
}