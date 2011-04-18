using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Autofac
{
    public class AutofacBootStrapper : AbstractBootStrapper
    {
        private IContainer container;

        public AutofacBootStrapper()
        {
        }

        public AutofacBootStrapper(IContainer container)
        {
            this.container = container;
        }

        protected IContainer Container
        {
            get { return container; }
        }

        protected override void ConfigureBusFacility(AbstractRhinoServiceBusConfiguration configuration)
        {
            configuration.UseAutofac(container);
        }

        public override void ExecuteDeploymentActions(string user)
        {
            foreach(var action in container.Resolve<IEnumerable<IDeploymentAction>>())
            {
                action.Execute(user);
            }
        }

        public override void ExecuteEnvironmentValidationActions()
        {
            foreach(var action in container.Resolve<IEnumerable<IEnvironmentValidationAction>>())
            {
                action.Execute();
            }
        }

        public override T GetInstance<T>()
        {
            return container.Resolve<T>();
        }

        public override void Dispose()
        {
            container.Dispose();
        }

        public override void CreateContainer()
        {
            if(container == null)
                container = new ContainerBuilder().Build();

            ConfigureContainer();
        }

        protected virtual void ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(Assembly)
                .AssignableTo<IDeploymentAction>()
                .SingleInstance();
            builder.RegisterAssemblyTypes(Assembly)
                .AssignableTo<IEnvironmentValidationAction>()
                .SingleInstance();
            
            builder.Update(container);
            
            RegisterConsumersFrom(Assembly);
        }

        protected virtual void RegisterConsumersFrom(Assembly assembly)
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(assembly)
                .Where(type =>
                    typeof(IMessageConsumer).IsAssignableFrom(type) &&
                        typeof(IOccasionalMessageConsumer).IsAssignableFrom(type) == false &&
                            IsTypeAcceptableForThisBootStrapper(type))
                .OnRegistered(e => ConfigureConsumer(e.ComponentRegistration))
                .InstancePerDependency();

            builder.Update(container);
        }

        protected virtual void ConfigureConsumer(IComponentRegistration registration)
        {
        }
    }
}