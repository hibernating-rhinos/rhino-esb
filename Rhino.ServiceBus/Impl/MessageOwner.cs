using System;
using System.Reflection;

namespace Rhino.ServiceBus.Impl
{
    public class MessageOwner
    {
        public string Name;
        public Uri Endpoint;

        public bool IsOwner(Type msg)
        {
            return msg.FullName.StartsWith(Name);
        }

    }
}