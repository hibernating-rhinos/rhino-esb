using System.Configuration;

namespace Rhino.ServiceBus.Config
{
    [ConfigurationCollection(typeof(MessageOwnerElement), CollectionType=ConfigurationElementCollectionType.BasicMap)]
    public class MessageOwnerElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MessageOwnerElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MessageOwnerElement) element).Endpoint;
        }

        public void Add(MessageOwnerElement messageOwner)
        {
            BaseAdd(messageOwner);
        }
    }
}