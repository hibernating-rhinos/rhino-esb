using System;
using System.Collections.Generic;
using System.Configuration;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Actions;
using System.Transactions;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Impl
{
	public class RhinoServiceBusFacility : AbstractRhinoServiceBusFacility
    {
        protected readonly List<MessageOwner> messageOwners = new List<MessageOwner>();

        protected override void ReadConfiguration()
	    {
	        ReadBusConfiguration();
	        new MessageOwnersConfigReader(FacilityConfig, messageOwners).ReadMessageOwners();
	    }

        protected override void RegisterComponents()
        {
            Kernel.Register(
                Component.For<IConsumerLocator>()
                    .ImplementedBy<CastleConsumerLocator>(),
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateLogQueueAction>(),
                Component.For<IDeploymentAction>()
                    .ImplementedBy<CreateQueuesAction>(),
                Component.For<IServiceBus, IStartableServiceBus>()
                    .ImplementedBy<DefaultServiceBus>()
                    .LifeStyle.Is(LifestyleType.Singleton)
                    .DependsOn(new
                    {
                        messageOwners = messageOwners.ToArray(),
                    })
                    .Parameters(Parameter.ForKey("modules").Eq(CreateModuleConfigurationNode())
                    )
                );
        }

        private IConfiguration CreateModuleConfigurationNode()
        {
            var config = new MutableConfiguration("array");
            foreach (Type type in messageModules)
            {
                config.CreateChild("item", "${" + type.FullName + "}");
            }
            return config;
        }

        protected void ReadBusConfiguration()
        {
            IConfiguration busConfig = FacilityConfig.Children["bus"];
            if (busConfig == null)
                throw new ConfigurationErrorsException("Could not find 'bus' node in configuration");

            string retries = busConfig.Attributes["numberOfRetries"];
            int result;
            if (int.TryParse(retries, out result))
                NumberOfRetries = result;

            string threads = busConfig.Attributes["threadCount"];
            if (int.TryParse(threads, out result))
                ThreadCount = result;

        	string isolationLevel = busConfig.Attributes["queueIsolationLevel"];
			if (!string.IsNullOrEmpty(isolationLevel))
				queueIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), isolationLevel);

            string inTransaction = busConfig.Attributes["consumeInTransaction"];
            bool boolResult;
            if (bool.TryParse(inTransaction, out boolResult))
                consumeInTxn = boolResult;

            string uriString = busConfig.Attributes["endpoint"];
            Uri endpoint;
            if (Uri.TryCreate(uriString, UriKind.Absolute, out endpoint) == false)
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'endpoint' on 'bus' has an invalid value '" + uriString + "'");
            }
            Endpoint = endpoint;

			string transactionalString = busConfig.Attributes["transactional"];
        	bool temp;
			if (bool.TryParse(transactionalString, out temp))
			{
				Transactional = temp ? TransactionalOptions.Transactional : TransactionalOptions.NonTransactional;
			}
			else if(transactionalString != null)
			{
				throw new ConfigurationErrorsException(
					"Attribute 'transactional' on 'bus' has an invalid value '" + uriString + "'");
			}
        }

		public IFacility UseFlatQueueStructure()
	    {
	        UseFlatQueue = true;
	        return this;
	    }
    }
}
