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

        public virtual IEnumerable<Assembly> Assemblies
        {
            get { return config.Assemblies; }
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