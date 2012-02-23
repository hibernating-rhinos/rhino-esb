using System;
using System.Runtime.Serialization;
using System.Security;
using System.Xml.Linq;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Convertors
{
    public class ThrowingWireEncryptedStringConvertor : IValueConvertor<WireEncryptedString>
    {
        public XElement ToElement(WireEncryptedString val, Func<Type, XNamespace> getNamespace)
        {
            throw new SecurityException(
                "Cannot send message containing WireEncryptedString when <security> was not properly set up");
        }

        public WireEncryptedString FromElement(XElement element)
        {
            var value = element.Element(XName.Get("Value", "System.String"));
            if(value==null)
                throw new SerializationException("<WireEncryptedString> did not have mandatory <Value> element");
            return new WireEncryptedString
            {
                Value = value.Value
            };
        }
    }
}
