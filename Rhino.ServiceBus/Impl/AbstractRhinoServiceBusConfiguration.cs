using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.MessageModules;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Serializers;
using System.Transactions;

namespace Rhino.ServiceBus.Impl
{
    /// <summary>
    /// Used to manage logging.
    /// </summary>
    public static class LogManager
    {
        static readonly ILog NullLogSingleton = new NullLog();
        static Func<Type, ILog> _logLocator = type => NullLogSingleton;

        /// <summary>
        /// Initializes the system with the specified log creator.
        /// </summary>
        /// <param name="logLocator">The log locator.</param>
        public static void Initialize(Func<Type, ILog> logLocator)
        {
            _logLocator = logLocator;
        }

        /// <summary>
        /// Creates a log.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static ILog GetLogger(Type type)
        {
            return _logLocator(type);
        }

        private class NullLog : ILog
        {
            public void Info(string message) { }
            public void Warn(string message) { }
            public void Warn(string message, Exception exception) { }
            public void Debug(string message) { }
            public void Debug(string message, Exception exception) { }
            public void Error(string message) { }
            public void Error(Exception exception) { }
            public void Error(string message, Exception exception) { }
            public void Fatal(string message) { }
            public void Fatal(string message, Exception exception) { }
        }
    }

    public static class LogExtensions
    {
        /// <summary>
        /// Logs the message as info.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="format">The message.</param>
        /// <param name="args">The args.</param>
        public static void InfoFormat(this ILog log, string format, params object[] args)
        {
            log.Info(string.Format(format, args));
        }

        /// <summary>
        /// Logs the message as info.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="format">The message.</param>
        /// <param name="args">The args.</param>
        /// <param name="e">The exception</param>
        public static void DebugFormat(this ILog log, Exception e, string format, params object[] args)
        {
            log.Debug(string.Format(format, args), e);
        }

        /// <summary>
        /// Logs the message as info.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="format">The message.</param>
        /// <param name="args">The args.</param>
        public static void DebugFormat(this ILog log, string format, params object[] args)
        {
            log.Debug(string.Format(format, args));
        }

        /// <summary>
        /// Logs the message as a warning.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="format">The message.</param>
        /// <param name="args">The args.</param>
        public static void WarnFormat(this ILog log, string format, params object[] args)
        {
            log.Info(string.Format(format, args));
        }

        /// <summary>
        /// Logs the message as an error.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="format">The message.</param>
        /// <param name="args">The args.</param>
        public static void ErrorFormat(this ILog log, string format, params object[] args)
        {
            log.Error(string.Format(format, args));
        }
    }

    public abstract class AbstractRhinoServiceBusConfiguration 
    {
        private readonly List<Type> messageModules = new List<Type>();
        private Type serializerImpl = typeof(XmlMessageSerializer);
        protected IsolationLevel queueIsolationLevel = IsolationLevel.Serializable;
        public bool consumeInTxn = true;
        private BusConfigurationSection configurationSection;
        private Action readConfiguration;
        private IBusContainerBuilder busContainerBuilder;


        protected AbstractRhinoServiceBusConfiguration()
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

        public BusConfigurationSection ConfigurationSection
        {
            get { return configurationSection; }
        }

        protected IBusContainerBuilder Builder
        {
            get { return busContainerBuilder; }
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

        public AbstractRhinoServiceBusConfiguration AddMessageModule<TModule>()
            where TModule : IMessageModule
        {
            messageModules.Add(typeof(TModule));
            return this;
        }

        public AbstractRhinoServiceBusConfiguration InsertMessageModuleAtFirst<TModule>()
            where TModule : IMessageModule
        {
            messageModules.Insert(0, typeof (TModule));
            return this;
        }

        public virtual void Configure()
        {
            ReadBusConfiguration();

            ApplyConfiguration();

            Builder.RegisterDefaultServices();
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

        public AbstractRhinoServiceBusConfiguration UseMessageSerializer<TMessageSerializer>()
        {
            serializerImpl = typeof(TMessageSerializer);
            return this;
        }

        public AbstractRhinoServiceBusConfiguration UseStandaloneConfigurationFile(string fileName)
        {
            readConfiguration = () =>
            {
                configurationSection = ConfigurationManager.OpenMappedMachineConfiguration(new ConfigurationFileMap(fileName)).GetSection("rhino.esb") as BusConfigurationSection;
            };
            return this;
        }

        public AbstractRhinoServiceBusConfiguration UseConfiguration(BusConfigurationSection busConfiguration)
        {
            configurationSection = busConfiguration;
            return this;
        }

        public AbstractRhinoServiceBusConfiguration DisableQueueAutoCreation()
        {
            DisableAutoQueueCreation = true;
            return this;
        }

        public void BuildWith(IBusContainerBuilder builder)
        {
            busContainerBuilder = builder;
            builder.WithInterceptor(new ConsumerInterceptor());
        }
    }
}