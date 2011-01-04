using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Castle.Config
{
    public class BusConfiguration : IBusConfigurationAware 
    {
        private readonly IWindsorContainer container;

        public BusConfiguration(IWindsorContainer container)
        {
            this.container = container;
        }

        public void Configure(AbstractRhinoServiceBusFacility config)
        {
            var busConfiguration = config as RhinoServiceBusFacility;
            if (busConfiguration == null)
                return;
            busConfiguration.ConfigurationComplete += () =>
            container.Register(
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateLogQueueAction>(),
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateQueuesAction>(),
                Component.For<IServiceBus, IStartableServiceBus>()
                    .ImplementedBy<DefaultServiceBus>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .DependsOn(new
                    {
                        messageOwners = busConfiguration.MessageOwners.ToArray()
                    })
                    .Parameters(
                        Parameter.ForKey("modules").Eq(CreateModuleConfigurationNode(busConfiguration.MessageModules))
                    )
                );
        }


        private static IConfiguration CreateModuleConfigurationNode(IEnumerable<Type> messageModules)
        {
            var config = new MutableConfiguration("array");
            foreach (Type type in messageModules)
            {
                config.CreateChild("item", "${" + type.FullName + "}");
            }
            return config;
        }
    }
}