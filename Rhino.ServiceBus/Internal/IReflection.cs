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

        void InvokeConsume(object consumer, object msg);

        Type[] GetMessagesConsumed(IMessageConsumer consumer);

        Type[] GetMessagesConsumed(Type consumerType, Predicate<Type> filter);

        object InvokeSagaPersisterGet(object persister, Guid correlationId);

        void InvokeSagaPersisterSave(object persister, object saga);

        void InvokeSagaPersisterComplete(object persister, object saga);

        string GetNamespaceForXml(Type type);

        string GetAssemblyQualifiedNameWithoutVersion(Type type);

        IEnumerable<string> GetProperties(object value);

        object Get(object instance, string name);

        Type GetType(string type);

        void InvokeAdd(object instance, object item);

        object InvokeFromElement(object covertor, XElement value);

        XElement InvokeToElement(object covertor, object value, Func<Type, XNamespace> getNamespace);
        string GetNameForXml(Type type);
    }
}