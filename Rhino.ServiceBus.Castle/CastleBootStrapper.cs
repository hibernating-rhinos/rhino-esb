using System.Reflection;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Castle
{
    public abstract class CastleBootStrapper : AbstractBootStrapper
    {
        private IWindsorContainer container;

        protected CastleBootStrapper()
        {
        }

        protected CastleBootStrapper(IWindsorContainer container)
        {
            this.container = container;
        }

        protected IWindsorContainer Container
        {
            get { return container; }
        }

        protected override void ConfigureBusFacility(AbstractRhinoServiceBusConfiguration configuration)
        {
            configuration.UseCastleWindsor(container);
        }

        public override void ExecuteDeploymentActions(string user)
        {
            foreach (var action in container.ResolveAll<IDeploymentAction>())
                action.Execute(user);
        }

        public override void ExecuteEnvironmentValidationActions()
        {
            foreach (var action in container.ResolveAll<IEnvironmentValidationAction>())
                action.Execute();
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
            if (container == null)
                container = new WindsorContainer();

            ConfigureContainer();
        }

        protected virtual void ConfigureContainer()
        {
            foreach (var assembly in Assemblies)
            {
                container.Register(
                     AllTypes.FromAssembly(assembly)
                         .BasedOn<IDeploymentAction>(),
                     AllTypes.FromAssembly(assembly)
                         .BasedOn<IEnvironmentValidationAction>()
                     );
                RegisterConsumersFrom(assembly);
            }
        }

        protected virtual void RegisterConsumersFrom(Assembly assembly)
        {
            container.RegisterConsumersFrom(assembly, ConfigureConsumer, IsTypeAcceptableForThisBootStrapper);
        }

        protected virtual void ConfigureConsumer(ComponentRegistration registration)
        {
            registration.Named(registration.Implementation.FullName);
        }
    }
}