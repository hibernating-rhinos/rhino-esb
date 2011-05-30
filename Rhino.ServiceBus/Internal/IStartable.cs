using System;

namespace Rhino.ServiceBus.Internal
{
    public interface IStartable : IDisposable
    {

        /// <summary>
        /// Register all message modules, subscribe to all the interesting messages and
        /// start the trasport. 
        /// This call will return after starting the bus, and the bus itself will be executed on
        /// a background thread.
        /// </summary>
        void Start();
    }
}