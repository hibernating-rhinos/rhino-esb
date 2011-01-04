using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.LoadBalancer;

namespace Rhino.ServiceBus.Castle.Config
{
    public class LoadBalancerConfiguration : IBusConfigurationAware
    {
        private readonly IWindsorContainer container;

        public LoadBalancerConfiguration(IWindsorContainer container)
        {
            this.container = container;
        }

        public void Configure(AbstractRhinoServiceBusFacility config)
        {
            var loadBalancerConfig = config as LoadBalancerFacility;
            if (loadBalancerConfig == null)
                return;
            if (loadBalancerConfig.SecondaryLoadBalancer != null)
            {
                container.Register(Component.For<MsmqLoadBalancer>()
                                       .ImplementedBy(loadBalancerConfig.LoadBalancerType)
                                       .LifeStyle.Is(LifestyleType.Singleton)
                                       .DependsOn(new
                                       {
                                           endpoint = loadBalancerConfig.Endpoint,
                                           threadCount = loadBalancerConfig.ThreadCount,
                                           primaryLoadBalancer = loadBalancerConfig.PrimaryLoadBalancer,
                                           transactional = loadBalancerConfig.Transactional,
                                           secondaryLoadBalancer = loadBalancerConfig.SecondaryLoadBalancer,
                                    }));
            }
            else
            {
                container.Register(Component.For<MsmqLoadBalancer>()
                                    .ImplementedBy(loadBalancerConfig.LoadBalancerType)
                                    .LifeStyle.Is(LifestyleType.Singleton)
                                    .DependsOn(new
                                    {
                                        endpoint = loadBalancerConfig.Endpoint,
                                        threadCount = loadBalancerConfig.ThreadCount,
                                        primaryLoadBalancer = loadBalancerConfig.PrimaryLoadBalancer,
                                        transactional = loadBalancerConfig.Transactional
                                    }));
            }
            container.Register(
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateLoadBalancerQueuesAction>()
                );

            if (loadBalancerConfig.ReadyForWork != null)
            {
                container.Register(Component.For<MsmqReadyForWorkListener>()
                                    .LifeStyle.Is(LifestyleType.Singleton)
                                    .DependsOn(new
                                    {
                                        endpoint = loadBalancerConfig.ReadyForWork,
                                        threadCount = loadBalancerConfig.ThreadCount,
                                        transactional = loadBalancerConfig.Transactional
                                    }));
                container.Register(
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateReadyForWorkQueuesAction>()
                );
            }
        }
    }
}