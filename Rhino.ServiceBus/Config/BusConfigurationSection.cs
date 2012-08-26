using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    public class BusConfigurationSection : ConfigurationSection
    {
        public BusConfigurationSection()
        {
            SetupDefaults();
        }

        public BusElement Bus
        {
            get { return this["bus"] as BusElement; }
        }

        public LoadBalancerElement LoadBalancer
        {
            get { return this["loadBalancer"] as LoadBalancerElement; }
        }

        public SecurityElement Security
        {
            get { return this["security"] as SecurityElement; }
        }

        public MessageOwnerElementCollection MessageOwners
        {
            get { return this["messages"] as MessageOwnerElementCollection; }
        }

        [ConfigurationProperty("assemblies")]
        public AssemblyElementCollection Assemblies
        {
            get { return this["assemblies"] as AssemblyElementCollection; }
            set { this["assemblies"] = value; }
        }

        private void SetupDefaults()
        {
            Properties.Add(new ConfigurationProperty("security", typeof(SecurityElement), null));
            Properties.Add(new ConfigurationProperty("bus", typeof(BusElement), null));
            Properties.Add(new ConfigurationProperty("loadBalancer", typeof(LoadBalancerElement), null));
            Properties.Add(new ConfigurationProperty("messages", typeof(MessageOwnerElementCollection), null));
            Properties.Add(new ConfigurationProperty("assemblies", typeof(AssemblyElementCollection), null));
        }
    }
}


        