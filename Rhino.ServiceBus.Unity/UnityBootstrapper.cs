using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Unity
{
    public abstract class UnityBootStrapper : AbstractBootStrapper
    {
        private IUnityContainer container;

        protected UnityBootStrapper()
        {
        }

        protected UnityBootStrapper(IUnityContainer container)
        {
            this.container = container;
        }

        protected IUnityContainer Container
        {
            get { return container; }
        }

        public override void CreateContainer()
        {
            if (container == null)
                container = new UnityContainer();

            ConfigureContainer();
        }

        protected virtual void ConfigureContainer()
        {
            foreach (var assembly in Assemblies)
            {
                container.RegisterTypesFromAssembly<IDeploymentAction>(assembly);
                container.RegisterTypesFromAssembly<IEnvironmentValidationAction>(assembly);
                ConfigureConsumers(assembly);
            }
        }

        protected virtual void ConfigureConsumers(Assembly assembly)
        {
            var consumers = assembly.GetTypes().Where(type =>
                                                      typeof(IMessageConsumer).IsAssignableFrom(type) &&
                                                      !typeof(IOccasionalMessageConsumer).IsAssignableFrom(type) &&
                                                      IsTypeAcceptableForThisBootStrapper(type)).ToList();
            consumers.ForEach(consumer => container.RegisterType(typeof(IMessageConsumer), consumer, consumer.FullName, new TransientLifetimeManager()));
        }

        protected override void ConfigureBusFacility(Impl.AbstractRhinoServiceBusConfiguration configuration)
        {
            configuration.UseUnity(container);
            base.ConfigureBusFacility(configuration);
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
    }
}