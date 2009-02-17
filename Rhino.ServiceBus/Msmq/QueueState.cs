using System.Messaging;
using System.Threading;

namespace Rhino.ServiceBus.Msmq
{
    public class QueueState
    {
        public MessageQueue Queue;
        public ManualResetEvent WaitHandle;
    }
}