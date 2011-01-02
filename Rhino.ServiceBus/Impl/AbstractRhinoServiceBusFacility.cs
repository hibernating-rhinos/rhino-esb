using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Serializers;
using System.Transactions;

namespace Rhino.ServiceBus.Impl
{
    public abstract class AbstractRhinoServiceBusFacility 
    {
        private readonly List<Type> messageModules = new List<Type>();
        private Type serializerImpl = typeof(XmlMessageSerializer);
        protected IsolationLevel queueIsolationLevel = IsolationLevel.Serializable;
        public bool consumeInTxn = true;
        private BusConfigurationSection configurationSection;
        private Action readConfiguration;


        protected AbstractRhinoServiceBusFacility()
        {
            readConfiguration = () =>
            {
                configurationSection = ConfigurationManager.GetSection("rhino.esb") as BusConfigurationSection;
            };
            ThreadCount = 1;
            NumberOfRetries = 5;
            Transactional = TransactionalOptions.FigureItOut;
        }

        public Uri Endpoint { get; set; }

        public int NumberOfRetries { get; set; }

        public int ThreadCount { get; set; }

        public bool UseFlatQueue { get; set; }

        public bool DisableAutoQueueCreation { get; set; }

		public TransactionalOptions Transactional { get; set; }

        public event Action ConfigurationStarted;

        public event Action ConfigurationComplete;

        public BusConfigurationSection ConfigurationSection
        {
            get { return configurationSection; }
        }

        public IsolationLevel IsolationLevel
        {
            get { return queueIsolationLevel; }
        }

        public bool ConsumeInTransaction
        {
            get { return consumeInTxn; }
        }

        public Type SerializerType
        {
            get { return serializerImpl; }
        }

        public IEnumerable<Type> MessageModules
        {
            get { return new ReadOnlyCollection<Type>(messageModules); }
        }

        public AbstractRhinoServiceBusFacility AddMessageModule<TModule>()
            where TModule : IMessageModule
        {
            messageModules.Add(typeof(TModule));
            return this;
        }

        public AbstractRhinoServiceBusFacility InsertMessageModuleAtFirst<TModule>()
            where TModule : IMessageModule
        {
            messageModules.Insert(0, typeof (TModule));
            return this;
        }

        public void Configure()
        {
            ReadBusConfiguration();

            ApplyConfiguration();

            var copy = ConfigurationStarted;
            if (copy != null)
                copy();

            var complete = ConfigurationComplete;
            if (complete != null)
                complete();
        }

        protected abstract void ApplyConfiguration();

        protected virtual void ReadBusConfiguration()
        {
            if (configurationSection != null)
                return;

            readConfiguration();

            if (configurationSection == null)
                throw new ConfigurationErrorsException("could not find rhino.esb configuration section");
        }

        public AbstractRhinoServiceBusFacility UseMessageSerializer<TMessageSerializer>()
        {
            serializerImpl = typeof(TMessageSerializer);
            return this;
        }

        public AbstractRhinoServiceBusFacility UseStandaloneConfigurationFile(string fileName)
        {
            readConfiguration = () =>
            {
                configurationSection = ConfigurationManager.OpenMappedMachineConfiguration(new ConfigurationFileMap(fileName)).GetSection("rhino.esb") as BusConfigurationSection;
            };
            return this;
        }

        public AbstractRhinoServiceBusFacility UseConfiguration(BusConfigurationSection busConfiguration)
        {
            configurationSection = busConfiguration;
            return this;
        }

        public AbstractRhinoServiceBusFacility DisableQueueAutoCreation()
        {
            DisableAutoQueueCreation = true;
            return this;
        }
    }
}