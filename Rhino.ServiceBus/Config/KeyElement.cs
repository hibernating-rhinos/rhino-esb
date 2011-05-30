using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    public class KeyElement : ConfigurationElement 
    {
        public string Value { get; private set; }

        protected override void DeserializeElement(System.Xml.XmlReader reader, bool serializeCollectionKey)
        {
            Value = reader.ReadElementContentAsString();
        }
    }
}