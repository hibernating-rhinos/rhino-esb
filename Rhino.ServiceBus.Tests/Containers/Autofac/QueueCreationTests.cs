using System;
using Rhino.ServiceBus.Autofac;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Msmq;
using Xunit;

namespace Rhino.ServiceBus.Tests.Containers.Autofac
{
    public class QueueCreationTests : IDisposable
    {
        private const string EndpointUri = "msmq://localhost/autofac-create.test";
        private readonly Endpoint _endpoint = new Endpoint {Uri = new Uri( EndpointUri )};

        [Fact]
        public void Endpoint_queue_is_created_on_start()
        {
            var host = new DefaultHost();
            host.BusConfiguration( config => config.Bus( EndpointUri ) );

            host.Start<AutofacTestBootStrapper>();
            host.Dispose();

            var endpointQueue = MsmqUtil.GetQueuePath( _endpoint );
            Assert.True( endpointQueue.Exists );
        }

        public void Dispose()
        {
            var endpointQueue = MsmqUtil.GetQueuePath( _endpoint );
            if ( endpointQueue.Exists )
            {
                endpointQueue.Delete();
            }
        }
    }

    public class AutofacTestBootStrapper : AutofacBootStrapper
    {
    }
}