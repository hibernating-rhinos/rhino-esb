using System;
using System.Xml.Linq;

namespace Rhino.ServiceBus.Internal
{
	public interface ICustomElementSerializer
	{
		bool CanSerialize(Type type);
		XElement ToElement(object val, Func<Type, XNamespace> getNamespace);
		object FromElement(Type type, XElement element);
	}
}
