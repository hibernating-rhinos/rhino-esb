using System;
using System.Xml.Linq;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Convertors
{
    public class WireEncryptedStringConvertor : IValueConvertor<WireEncryptedString>
    {
    	public IEncryptionService EncryptionService { get; set;}

        public WireEncryptedStringConvertor(IEncryptionService encryptionService)
        {
        	EncryptionService = encryptionService;
        }

        public XElement ToElement(WireEncryptedString val, Func<Type, XNamespace> getNamespace)
        {
        	var encryptedValue = EncryptionService.Encrypt(val);
			return new XElement(getNamespace(typeof(string)) + "Value",
				new XAttribute("iv", encryptedValue.Base64Iv),
				encryptedValue.EncryptedBase64Value);
        }

        public WireEncryptedString FromElement(XElement element)
        {
            var value = element.Element(XName.Get("Value","System.String"));
            if(value==null)
                throw new ArgumentException("element must contain <value> element");
            
            var attribute = value.Attribute("iv");
            if(attribute==null)
                throw new ArgumentException("element must contain a <value> element with iv attribute");

        	var encryptedValue = new EncryptedValue
        	{
				EncryptedBase64Value = element.Value,
				Base64Iv = attribute.Value,
        	};

        	return EncryptionService.Decrypt(encryptedValue);
        }
    }
}
