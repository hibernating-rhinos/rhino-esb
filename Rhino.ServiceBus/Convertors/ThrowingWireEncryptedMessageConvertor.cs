using System;
using System.Security;
using System.Xml.Linq;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.Convertors
{
	public class ThrowingWireEncryptedMessageConvertor : IElementSerializationBehavior
	{
		private readonly Type wireEncryptedMessageType = typeof (IWireEncryptedMessage);

		public bool ShouldApplyBehavior(Type type)
		{
			return wireEncryptedMessageType.IsAssignableFrom(type);
		}

		public XElement ApplyElementBehavior(XElement element)
		{
			throw new SecurityException(
				"Cannot send IWireEncryptedMessage when <security> was not properly set up");
		}

		public XElement RemoveElementBehavior(XElement element)
		{
			throw new SecurityException(
				"Cannot accept IWireEncryptedMessage when <security> was not properly set up");
		}
	}
}
