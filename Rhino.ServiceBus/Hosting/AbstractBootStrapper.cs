using System;
using System.Reflection;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Hosting
{
    public abstract class AbstractBootStrapper : IDisposable
    {
        private AbstractRhinoServiceBusConfiguration config;

        public virtual Assembly Assembly
        {
            get { return GetType().Assembly; }
        }

        public virtual void InitializeContainer()
        {
            CreateContainer();
            config = CreateConfiguration();
            ConfigureBusFacility(config);
        }

        public virtual void UseConfiguration(BusConfigurationSection configurationSection)
        {
            config.UseConfiguration(configurationSection);
        }

        public abstract void CreateContainer();

        public abstract void ExecuteDeploymentActions(string user);

        public abstract void ExecuteEnvironmentValidationActions();

        public abstract T GetInstance<T>();

    	protected virtual bool IsTypeAcceptableForThisBootStrapper(Type t)
        {
            return true;
        }

        protected virtual AbstractRhinoServiceBusConfiguration CreateConfiguration()
        {
            return new RhinoServiceBusConfiguration();
        }

        protected virtual void ConfigureBusFacility(AbstractRhinoServiceBusConfiguration configuration)
        {
        }

        public virtual void BeforeStart()
        {
            config.Configure();
        }

        public virtual void AfterStart()
        {
        }

        public abstract void Dispose();
    }
}