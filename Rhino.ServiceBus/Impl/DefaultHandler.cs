using System;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Impl
{
    public class DefaultHandler : IHandler
    {
        private readonly Func<object> resolveAction;

        public DefaultHandler(Type service, Type implementation, Func<object> resolveAction)
        {
            this.resolveAction = resolveAction;
            Implementation = implementation;
            Service = service;
        }

        public Type Implementation { get; private set; }
        public Type Service { get; private set; }

        public object Resolve()
        {
            return resolveAction();
        }
    }
}