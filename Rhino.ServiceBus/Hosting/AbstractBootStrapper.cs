using System;
using System.Reflection;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Hosting
{
    public abstract class AbstractBootStrapper : IDisposable
    {
        protected IWindsorContainer container;

        public virtual Assembly Assembly
        {
            get { return GetType().Assembly; }
        }

        public virtual void AfterStart()
        {
        }

        public void InitializeContainer(IWindsorContainer windsorContainer)
        {
            container = windsorContainer;

            ConfigureContainer();
        }

        protected virtual void ConfigureContainer()
        {
            container.Register(
                AllTypes.Of<IDeploymentAction>()
                    .FromAssembly(Assembly),
                AllTypes.Of<IEnvironmentValidationAction>()
                    .FromAssembly(Assembly)
                );
			RegisterConsumersFrom (Assembly);
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
						registration.LifeStyle.Is (LifestyleType.Transient);
						ConfigureConsumer (registration);
					})
				);
		}

    	protected virtual void ConfigureConsumer(ComponentRegistration registration)
    	{
    		registration.Named(registration.Implementation.Name);
    	}

    	protected virtual bool IsTypeAcceptableForThisBootStrapper(Type t)
        {
            return true;
        }

        public virtual void BeforeStart()
        {
            
        }

        public virtual void ConfigureBusFacility(RhinoServiceBusFacility facility)
        {
            
        }

        public virtual void Dispose()
        {
            container.Dispose();
        }
    }
}