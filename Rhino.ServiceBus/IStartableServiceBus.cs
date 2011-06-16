using System;
using Rhino.ServiceBus.Internal;

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
    public interface IStartableServiceBus : IServiceBus, IStartable
    {

    }
}