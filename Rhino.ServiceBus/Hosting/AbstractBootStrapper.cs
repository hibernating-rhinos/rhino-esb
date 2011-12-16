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

        [Obsolete("BeforeStart is now obsolete, please use BeginStart, or if overriding use OnBeginStart")]
        public virtual void BeforeStart()
        {
            config.Configure();
        }

        [Obsolete("AfterStart is now obsolete, please use EndStart, or if overriding use OnEndStart")]
        public virtual void AfterStart()
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