using System;
using System.Collections.Generic;
using System.Configuration;
using Castle.Core.Configuration;

namespace Rhino.ServiceBus.Impl
{
    public class MessageOwnersConfigReader
    {
        private readonly IConfiguration configuration;
        private readonly ICollection<MessageOwner> messageOwners;

        public MessageOwnersConfigReader(IConfiguration configuration, ICollection<MessageOwner> messageOwners)
        {
            this.configuration = configuration;
            this.messageOwners = messageOwners;
        }

        public void ReadMessageOwners()
        {
            IConfiguration messageConfig = configuration.Children["messages"];
            if (messageConfig == null)
                throw new ConfigurationErrorsException("Could not find 'messages' node in configuration");

            foreach (IConfiguration child in messageConfig.Children)
            {
                if (child.Name != "add")
                    throw new ConfigurationErrorsException("Unknown node 'messages/" + child.Name + "'");

                string msgName = child.Attributes["name"];
                if (string.IsNullOrEmpty(msgName))
                    throw new ConfigurationErrorsException("Invalid name element in the <messages/> element");

                string uriString = child.Attributes["endpoint"];
                Uri ownerEndpoint;
                try
                {
                    ownerEndpoint = new Uri(uriString);
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException("Invalid endpoint url: " + uriString, e);
                }

                bool? transactional = null;
                string transactionalString = child.Attributes["transactional"];
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