using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.MessageModules;

namespace Rhino.ServiceBus.Castle.Config
{
    public class LoadBalancerEndpointConfiguration : IBusConfigurationAware 
    {
        private readonly IWindsorContainer container;

        public LoadBalancerEndpointConfiguration(IWindsorContainer container)
        {
            this.container = container;
        }

        public void Configure(AbstractRhinoServiceBusFacility config)
        {
            var busConfig = config as RhinoServiceBusFacility;
            if (busConfig == null)
                return;
            var loadBalancerReader = new LoadBalancerConfigurationReader(config);
            if (loadBalancerReader.LoadBalancerEndpoint == null)
                return;

            var endpoint = new Endpoint { Uri = loadBalancerReader.LoadBalancerEndpoint };
            container.Register(
                Component.For<LoadBalancerMessageModule>()
                    .DependsOn(new { loadBalancerEndpoint = endpoint.Uri })
                );
            config.AddMessageModule<LoadBalancerMessageModule>();
        }
    }
}