using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    public class SecurityElement : ConfigurationElement 
    {
        public SecurityElement()
        {
            SetupDefaults();
        }

        private void SetupDefaults()
        {
            Properties.Add(new ConfigurationProperty("key", typeof(KeyElement), null));
        }

        public string Key
        {
            get { return ((KeyElement) this["key"]).Value; }
        }
    }
}