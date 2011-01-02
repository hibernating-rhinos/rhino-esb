using System;
using System.Reflection;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Hosting
{
    public abstract class AbstractBootStrapper : IDisposable
    {
        private RhinoServiceBusFacility config;

        public virtual Assembly Assembly
        {
            get { return GetType().Assembly; }
        }

        public virtual void AfterStart()
        {
        }

        public virtual void InitializeContainer()
        {
            CreateContainer();
            CreateConfiguration();
            config.Configure();
        }

        public virtual void UseConfiguration(BusConfigurationSection configurationSection)
        {
            config.UseConfiguration(configurationSection);
        }

        protected abstract void CreateContainer();

        public abstract void ExecuteDeploymentActions(string user);

        public abstract void ExecuteEnvironmentValidationActions();

        public abstract IStartableServiceBus GetStartableServiceBus();

    	protected virtual bool IsTypeAcceptableForThisBootStrapper(Type t)
        {
            return true;
        }

        protected virtual void CreateConfiguration()
        {
            config = new RhinoServiceBusFacility();
            ConfigureBusFacility(config);
        }

        protected virtual void ConfigureBusFacility(RhinoServiceBusFacility facility)
        {
        }

        public virtual void BeforeStart()
        {
        }

        public abstract void Dispose();
    }
}