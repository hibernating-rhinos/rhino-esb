using System;
using System.Collections.Generic;
using System.Configuration;
using Rhino.ServiceBus.Config;

namespace Rhino.ServiceBus.Impl
{
    public class MessageOwnersConfigReader
    {
        private readonly BusConfigurationSection configuration;
        private readonly ICollection<MessageOwner> messageOwners;

        public MessageOwnersConfigReader(BusConfigurationSection configuration, ICollection<MessageOwner> messageOwners)
        {
            this.configuration = configuration;
            this.messageOwners = messageOwners;
        }
        public string EndpointScheme { get; private set; }
        public void ReadMessageOwners()
        {
            var messageConfig = configuration.MessageOwners;
            if (messageConfig == null)
                throw new ConfigurationErrorsException("Could not find 'messages' node in configuration");

            foreach (MessageOwnerElement child in messageConfig)
            {
                string msgName = child.Name;
                if (string.IsNullOrEmpty(msgName))
                    throw new ConfigurationErrorsException("Invalid name element in the <messages/> element");

                string uriString = child.Endpoint;
                Uri ownerEndpoint;
                try
                {
                    ownerEndpoint = new Uri(uriString);
                    if(EndpointScheme==null)
                    {
                        EndpointScheme = ownerEndpoint.Scheme;
                    }
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException("Invalid endpoint url: " + uriString, e);
                }

                bool? transactional = null;
                string transactionalString = child.Transactional;
                if (string.IsNullOrEmpty(transactionalString) == false)
                {
                    bool temp;
                    if (bool.TryParse(transactionalString, out temp) == false)
                        throw new ConfigurationErrorsException("Invalid transactional settings: " + transactionalString);
                    transactional = temp;
                }
                var endpoint = new Endpoint {Uri = ownerEndpoint};
                messageOwners.Add(new MessageOwner
                                      {
                                          Name = msgName,
                                          Endpoint = endpoint.Uri,
                                          Transactional = transactional
                                      });
            }
        }
    }
}