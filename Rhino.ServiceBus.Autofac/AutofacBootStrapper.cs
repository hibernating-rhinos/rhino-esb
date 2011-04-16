using Autofac;
using Rhino.ServiceBus.Hosting;

namespace Rhino.ServiceBus.Autofac
{
    public class AutofacBootStrapper : AbstractBootStrapper
    {
        private readonly IContainer container;

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

        public override void CreateContainer()
        {
        }

        protected virtual void ConfigureContainer()
        {
            
        }

        public override void ExecuteDeploymentActions(string user)
        {
        }

        public override void ExecuteEnvironmentValidationActions()
        {
        }

        public override T GetInstance<T>()
        {
            return default(T);
        }

        public override void Dispose()
        {
        }
    }
}