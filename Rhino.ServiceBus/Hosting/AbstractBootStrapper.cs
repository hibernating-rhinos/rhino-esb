using System;
using System.Reflection;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Hosting
{
    public abstract class AbstractBootStrapper : IDisposable
    {
        private AbstractRhinoServiceBusConfiguration config;
        private BusConfigurationSection busSection;

        public virtual IEnumerable<Assembly> Assemblies
        {
            get { yield return GetType().Assembly; }
        }

        public virtual void InitializeContainer()
        {
            config = CreateConfiguration();
            CreateContainer();
            ConfigureBusFacility(config);
        }

        public virtual void UseConfiguration(BusConfigurationSection configurationSection)
        {
            busSection = configurationSection;
            if (config != null) config.UseConfiguration(busSection);
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
            var cfg = new RhinoServiceBusConfiguration();
            if (busSection!=null) cfg.UseConfiguration(busSection);
            return cfg;
        }

        protected virtual void ConfigureBusFacility(AbstractRhinoServiceBusConfiguration configuration)
        {
        }

        protected virtual void OnBeginStart()
        {
        }

        public void BeginStart()
        {
            OnBeginStart();
            config.Configure();
        }

        public void EndStart()
        {
            OnEndStart();
        }

        protected virtual void OnEndStart()
        {
        }

        public abstract void Dispose();
    }
}
