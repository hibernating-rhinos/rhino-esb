using System;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Impl
{
    public class EndpointRouter : IEndpointRouter
    {
        private readonly Hashtable<Uri, Uri> mapping = new Hashtable<Uri, Uri>();

        public void Init()
        {
        }

        public void RemapEndpoint(Uri originalEndpoint, Uri newEndpoint)
        {
            mapping.Write(writer => writer.Add(originalEndpoint, newEndpoint));
        }

        public Endpoint GetRoutedEndpoint(Uri endpoint)
        {
            Uri newEndpoint = null;
            mapping.Read(reader => reader.TryGetValue(endpoint, out newEndpoint));
            return new Endpoint
            {
                Uri = newEndpoint ?? endpoint
            };
        }
    }
}