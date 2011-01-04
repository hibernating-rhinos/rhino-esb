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
    public class CastleBootStrapper : AbstractBootStrapper
    {
        private IWindsorContainer container;

        public CastleBootStrapper()
        {
        }

        public CastleBootStrapper(IWindsorContainer container)
        {
            this.container = container;
        }

        protected IWindsorContainer Container
        {
            get { return container; }
        }

        protected override void ConfigureBusFacility(AbstractRhinoServiceBusFacility facility)
        {
            facility.UseCastleWindsor(container);
        }

        public override void ExecuteDeploymentActions(string user)
        {
            foreach (var action in container.ResolveAll<IDeploymentAction>())
            {
                action.Execute(user);
            }
        }

        public override void ExecuteEnvironmentValidationActions()
        {
            foreach (var action in container.ResolveAll<IEnvironmentValidationAction>())
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
            if (container == null)
                container = new WindsorContainer();

            ConfigureContainer();
        }

        protected virtual void ConfigureContainer()
        {
            container.Register(
                 AllTypes.FromAssembly(Assembly)
                     .BasedOn<IDeploymentAction>(),
                 AllTypes.FromAssembly(Assembly)
                     .BasedOn<IEnvironmentValidationAction>()
                 );
            RegisterConsumersFrom(Assembly);
        }

        protected virtual void RegisterConsumersFrom(Assembly assembly)
		{
			container.Register (
				 AllTypes
					.FromAssembly (assembly)
					.Where (type =>
						typeof (IMessageConsumer).IsAssignableFrom (type) &&
						typeof (IOccasionalMessageConsumer).IsAssignableFrom (type) == false &&
						IsTypeAcceptableForThisBootStrapper (type)
					)
					.Configure (registration =>
					{
						registration.LifeStyle.Is(LifestyleType.Transient);
						ConfigureConsumer (registration);
					})
				);
		}

    	protected virtual void ConfigureConsumer(ComponentRegistration registration)
    	{
    		registration.Named(registration.Implementation.Name);
    	}
    }
}