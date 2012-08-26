using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    [ConfigurationCollection(typeof(AssemblyElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class AssemblyElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new AssemblyElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var assemblyElement = (AssemblyElement)element;
            return assemblyElement.Name;
        }

        public void Add(AssemblyElement assembly)
        {
            BaseAdd(assembly);
        }
    }
}