using System;
using System.Xml.Linq;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.DataStructures;

namespace Rhino.ServiceBus.Convertors
{
	public class WireEncryptedMessageConvertor : IElementSerializationBehavior
	{
		private readonly Type wireEncryptedMessageType = typeof (IWireEncryptedMessage);
		public IEncryptionService EncryptionService { get; set; }

		public WireEncryptedMessageConvertor(IEncryptionService encryptionService)
		{
			EncryptionService = encryptionService;
		}

		public bool ShouldApplyBehavior(Type type)
		{
			return wireEncryptedMessageType.IsAssignableFrom(type);
		}

		public XElement ApplyElementBehavior(XElement element)
		{
			var encryptedValue = EncryptionService.Encrypt(element.ToString());
			var replacement = new XElement(element.Name,
				new XElement(XName.Get("Value", "string"),
				new XAttribute("iv", encryptedValue.Base64Iv),
				encryptedValue.EncryptedBase64Value
				));
			return replacement;
		}

		public XElement RemoveElementBehavior(XElement element)
		{
			var value = element.Element(XName.Get("Value", "string"));
			if (value == null)
				throw new ArgumentException("element must contain <value> element");

			var attribute = value.Attribute("iv");
			if (attribute == null)
				throw new ArgumentException("element must contain a <value> element with iv attribute");

			var encryptedValue = new EncryptedValue
			{
                Base64Iv = attribute.Value,
				EncryptedBase64Value = element.Value,
			};
			var unencryptedValue = EncryptionService.Decrypt(encryptedValue);
			return XElement.Parse(unencryptedValue);
		}
	}
}
