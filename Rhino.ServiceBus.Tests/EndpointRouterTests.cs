using System;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Utils;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class EndpointRouterTests
    {

        [Fact]
        public void can_handle_localhost_consistently()
        {
            var router = new EndpointRouter();
            var uri = new Uri("http://localhost/blahdee");
            var normalizedUri = uri.NormalizeLocalhost();
            var routeTo = new Uri("http://remotehost/zippee");

            router.RemapEndpoint(uri, routeTo);

            Assert.AreEqual(routeTo, router.GetRoutedEndpoint(normalizedUri).Uri);
            Assert.AreEqual(routeTo, router.GetRoutedEndpoint(uri).Uri);
        }

        [Fact]
        public void can_handle_localhost_consistently_2()
        {
            var router = new EndpointRouter();
            var uri = new Uri("http://localhost/blahdee");
            var normalizedUri = uri.NormalizeLocalhost();
            var routeFrom = new Uri("http://remotehost/zippee");

            router.RemapEndpoint(routeFrom, uri);

            Assert.AreEqual(normalizedUri, router.GetRoutedEndpoint(routeFrom).Uri);
        }
    }
}