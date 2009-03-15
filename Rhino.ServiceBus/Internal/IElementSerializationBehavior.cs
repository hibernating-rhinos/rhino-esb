using System;
using System.Xml.Linq;

namespace Rhino.ServiceBus.Internal
{
	public interface IElementSerializationBehavior
	{
		bool ShouldApplyBehavior(Type type);
		XElement ApplyElementBehavior(XElement element);
		XElement RemoveElementBehavior(XElement element);
	}
}
