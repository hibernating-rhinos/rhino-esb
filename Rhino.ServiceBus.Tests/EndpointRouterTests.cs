using System;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Util;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class EndpointRouterTests
    {

        [Fact]
        public void can_handle_localhost_consistently()
        {
            var router = new EndpointRouter();
            var uri = new Uri("http://lOcAlHoSt/blahdee");
            var normalizedUri = uri.NormalizeLocalhost();
            var routeTo = new Uri("http://remotehost/zippee");

            router.RemapEndpoint(uri, routeTo);

            Assert.Equal(routeTo, router.GetRoutedEndpoint(normalizedUri).Uri);
            Assert.Equal(routeTo, router.GetRoutedEndpoint(uri).Uri);
        }

        [Fact]
        public void can_handle_localhost_consistently_2()
        {
            var router = new EndpointRouter();
            var uri = new Uri("http://127.0.0.1/blahdee");
            var normalizedUri = uri.NormalizeLocalhost();
            var routeFrom = new Uri("http://remotehost/zippee");

            router.RemapEndpoint(routeFrom, uri);

            Assert.Equal(normalizedUri, router.GetRoutedEndpoint(routeFrom).Uri);
        }
    }
}