using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Rhino.ServiceBus.Internal
{
    public interface IReflection
    {
        object CreateInstance(Type type, params object[]args);

        void Set(object instance, string name, Func<Type,object> generateValue);

        Type GetGenericTypeOf(Type type, object msg);

        Type GetGenericTypeOf(Type type, Type paramType);

		Type GetGenericTypeOf(Type type, params Type[] paramTypes);

		ICollection<Type> GetGenericTypesOfWithBaseTypes(Type type, object msg);

		ICollection<Type> GetGenericTypesOfWithBaseTypes(Type type, Type paramType);

        void InvokeConsume(object consumer, object msg);

        Type[] GetMessagesConsumed(IMessageConsumer consumer);

        Type[] GetMessagesConsumed(Type consumerType, Predicate<Type> filter);

        object InvokeSagaPersisterGet(object persister, Guid correlationId);

        void InvokeSagaPersisterSave(object persister, object saga);

        void InvokeSagaPersisterComplete(object persister, object saga);

		object InvokeSagaFinderFindBy(object sagaFinder, object msg);

        string GetNamespacePrefixForXml(Type type);

        string GetNamespaceForXml(Type type);

        IEnumerable<string> GetProperties(object value);

        object Get(object instance, string name);

        Type GetTypeFromXmlNamespace(string xmlNamespace);

        void InvokeAdd(object instance, object item);

        object InvokeFromElement(object covertor, XElement value);

        XElement InvokeToElement(object covertor, object value, Func<Type, XNamespace> getNamespace);
        string GetNameForXml(Type type);
    }
}
