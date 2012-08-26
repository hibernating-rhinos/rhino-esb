using System.Configuration;
using System.ComponentModel;
using System.Reflection;

namespace Rhino.ServiceBus.Config
{
    public class AssemblyElement : ConfigurationElement
    {
        [ConfigurationProperty("name")]
        public string Name
        {
            get { string name; return (!string.IsNullOrEmpty(name = (string)this["name"]) ? name : (Assembly != null ? Assembly.FullName : null)); }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("assembly", IsRequired = true)]
        [TypeConverter(typeof(AssemblyNameConverter))]
        public Assembly Assembly
        {
            get { return (Assembly)this["assembly"]; }
            set { base["assembly"] = value; }
        }
    }
}