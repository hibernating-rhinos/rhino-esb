using System;

namespace Rhino.ServiceBus.Tests
{
    public static class TestExtensions
    {
        public static Endpoint ToEndpoint(this Uri uri)
        {
            return new Endpoint
            {
                Uri = uri
            };
        }
    }
}