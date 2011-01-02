using System;

namespace Rhino.ServiceBus.Internal
{
    public interface IHandler
    {
        Type Service { get; }
        Type Implementation { get; }
        object Resolve();
    }
}