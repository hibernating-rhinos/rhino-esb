using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    public class SecurityElement : ConfigurationElement 
    {
        public SecurityElement()
        {
            SetupDefaults();
        }

        public string Key
        {
            get { return ((KeyElement) this["key"]).Value; }
            set { this["key"] = new KeyElement{Value = value}; }
        }

        private void SetupDefaults()
        {
            Properties.Add(new ConfigurationProperty("key", typeof(KeyElement), null));
        }
    }
}