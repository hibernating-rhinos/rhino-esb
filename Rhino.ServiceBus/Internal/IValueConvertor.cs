using System;
using System.Xml.Linq;

namespace Rhino.ServiceBus.Internal
{
    public interface IValueConvertor<T>
    {
        XElement ToElement(T val, Func<Type, XNamespace> getNamespace);
        T FromElement(XElement element);
    }
}