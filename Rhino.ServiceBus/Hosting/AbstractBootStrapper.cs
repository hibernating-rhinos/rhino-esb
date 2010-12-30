using System;
using System.Reflection;

namespace Rhino.ServiceBus.Hosting
{
    public abstract class AbstractBootStrapper : IDisposable
    {
        public virtual Assembly Assembly
        {
            get { return GetType().Assembly; }
        }

        public virtual void AfterStart()
        {
        }

        public abstract void InitializeContainer();

        public abstract void ExecuteDeploymentActions(string user);

        public abstract void ExecuteEnvironmentValidationActions();

        public abstract IStartableServiceBus GetStartableServiceBus();

    	protected virtual bool IsTypeAcceptableForThisBootStrapper(Type t)
        {
            return true;
        }

        public virtual void BeforeStart()
        {
        }

        public abstract void Dispose();
    }
}