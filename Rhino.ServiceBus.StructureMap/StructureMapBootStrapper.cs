using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using StructureMap;

namespace Rhino.ServiceBus.StructureMap
{
    public class StructureMapBootStrapper : AbstractBootStrapper 
    {
        private IContainer container;

        public StructureMapBootStrapper()
        {
        }

        public StructureMapBootStrapper(IContainer container)
        {
            this.container = container;
        }

        protected IContainer Container
        {
            get { return container; }
        }

        protected override void ConfigureBusFacility(AbstractRhinoServiceBusFacility facility)
        {
            facility.UseStructureMap(container);
            base.ConfigureBusFacility(facility);
        }

        public override void CreateContainer()
        {
            if (container == null)
                container = ObjectFactory.Container;

            ConfigureContainer();
        }

        private void ConfigureContainer()
        {
            container.Configure(c => c.Scan(s =>
            {
                s.Assembly(typeof(StructureMapBootStrapper).Assembly);
                s.AddAllTypesOf(typeof(IMessageConsumer)).NameBy(t => t.FullName);
                s.Exclude(t => typeof(IOccasionalMessageConsumer).IsAssignableFrom(t) == false);
                s.AddAllTypesOf<IDeploymentAction>();
                s.AddAllTypesOf<IEnvironmentValidationAction>();
            }));
        }

        public override void ExecuteDeploymentActions(string user)
        {
            foreach (var action in container.GetAllInstances<IDeploymentAction>())
            {
                action.Execute(user);
            }
        }

        public override void ExecuteEnvironmentValidationActions()
        {
            foreach (var action in container.GetAllInstances<IEnvironmentValidationAction>())
            {
                action.Execute();
            }
        }

        public override T GetInstance<T>()
        {
            return container.GetInstance<T>();
        }

        public override void Dispose()
        {
            container.Dispose();
        }
    }
}