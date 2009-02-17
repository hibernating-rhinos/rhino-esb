using System;

namespace Rhino.ServiceBus.Internal
{
    public interface IEndpointRouter
    {
        void Init();

        void RemapEndpoint(Uri originalEndpoint, Uri newEndpoint);

        Endpoint GetRoutedEndpoint(Uri endpoint);
    }
}