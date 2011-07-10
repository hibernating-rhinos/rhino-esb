using System;
using System.Linq;

using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

using Spring.Context;
using Spring.Context.Support;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;

namespace Rhino.ServiceBus.Spring
{
    [CLSCompliant(false)]
    public abstract class SpringBootStrapper : AbstractBootStrapper
    {
        private IConfigurableApplicationContext applicationContext;

        public SpringBootStrapper()
        {
        }

        public SpringBootStrapper(IConfigurableApplicationContext applicationContext)
        {
            this.applicationContext = applicationContext;
        }

        protected IConfigurableApplicationContext ApplicationContext
        {
            get { return applicationContext; }
        }

        protected override void ConfigureBusFacility(AbstractRhinoServiceBusConfiguration configuration)
        {
            configuration.UseSpring(applicationContext);
        }

        public override void ExecuteDeploymentActions(string user)
        {
            foreach (IDeploymentAction action in applicationContext.GetAll<IDeploymentAction>())
            {
                action.Execute(user);
            }
        }

        public override void ExecuteEnvironmentValidationActions()
        {
            foreach (IEnvironmentValidationAction action in applicationContext.GetAll<IEnvironmentValidationAction>())
            {
                action.Execute();
            }
        }

        public override T GetInstance<T>()
        {
            return applicationContext.Get<T>();
        }

        public override void Dispose()
        {
            applicationContext.Dispose();
        }

        public override void CreateContainer()
        {
            if (applicationContext == null)
                applicationContext = new StaticApplicationContext();

            ConfigureContainer();
        }

        protected virtual void ConfigureContainer()
        {
            applicationContext.RegisterSingletons<IDeploymentAction>(Assembly);
            applicationContext.RegisterSingletons<IEnvironmentValidationAction>(Assembly);
            RegisterConsumers();
        }

        protected virtual void RegisterConsumers()
        {
            Assembly.GetTypes()
                .Where(t => typeof (IMessageConsumer).IsAssignableFrom(t)
                            && !typeof (IOccasionalMessageConsumer).IsAssignableFrom(t)
                            && IsTypeAcceptableForThisBootStrapper(t)
                            && !t.IsInterface
                            && !t.IsAbstract)
                .ToList()
                .ForEach(type =>
                             {
                                 ObjectDefinitionBuilder definitionBuilder = ObjectDefinitionBuilder
                                     .RootObjectDefinition(new DefaultObjectDefinitionFactory(), type)
                                     .SetAutowireMode(AutoWiringMode.Constructor)
                                     .SetSingleton(false);
                                 applicationContext.ObjectFactory.RegisterObjectDefinition(type.FullName, definitionBuilder.ObjectDefinition);
                             }
                );
        }
    }
}