using System;
using System.Runtime.Serialization;
using System.Security;
using System.Xml.Linq;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Convertors
{
    public class ThrowingWireEcryptedStringConvertor : IValueConvertor<WireEcryptedString>
    {
        public XElement ToElement(WireEcryptedString val, Func<Type, XNamespace> getNamespace)
        {
            throw new SecurityException(
                "Cannot send message containing WireEcryptedString when <security> was not properly set up");
        }

        public WireEcryptedString FromElement(XElement element)
        {
            var value = element.Element(XName.Get("Value", "System.String"));
            if(value==null)
                throw new SerializationException("<WireEcryptedString> did not have mandatory <Value> element");
            return new WireEcryptedString
            {
                Value = value.Value
            };
        }
    }
}
