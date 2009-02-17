using System;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus
{
    /// <summary>
    /// Provide a way to start and dispose of the bus
    /// </summary>
    /// <remarks>
    /// It is expected that this will be used only in the application startup, by the 
    /// initialization code. Consumer code bases should use <see cref="IServiceBus"/> and
    /// not <see cref="IStartableServiceBus"/>.
    /// </remarks>
    public interface IStartableServiceBus : IServiceBus, IDisposable
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