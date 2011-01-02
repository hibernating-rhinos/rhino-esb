using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    public class MessageOwnerElement : ConfigurationElement
    {
        [ConfigurationProperty("name")]
        public string Name
        {
            get { return (string) this["name"]; }
        }

        [ConfigurationProperty("endpoint")]
        public string Endpoint
        {
            get { return (string) this["endpoint"]; }
        }

        [ConfigurationProperty("transactional")]
        public string Transactional
        {
            get { return (string) this["transactional"]; }
        }
    }
}