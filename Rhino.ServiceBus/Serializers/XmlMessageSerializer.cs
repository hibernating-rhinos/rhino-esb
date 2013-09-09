using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Caching;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Internal;
using System.Linq;
using Rhino.ServiceBus.DataStructures;

namespace Rhino.ServiceBus.Serializers
{
    public class XmlMessageSerializer : IMessageSerializer
    {
        private const int MaxNumberOfAllowedItemsInCollection = 256;
        private readonly IReflection reflection;
        private readonly IServiceLocator serviceLocator;
        private readonly Hashtable<Type, bool> typeHasConvertorCache = new Hashtable<Type, bool>();
    	private ICustomElementSerializer[] customElementSerializers;
    	private IElementSerializationBehavior[] elementSerializationBehaviors;

    	public XmlMessageSerializer(IReflection reflection, IServiceLocator serviceLocator )
        {
            this.reflection = reflection;
            this.serviceLocator = serviceLocator;
        	customElementSerializers = this.serviceLocator.ResolveAll<ICustomElementSerializer>().ToArray();
    		elementSerializationBehaviors = this.serviceLocator.ResolveAll<IElementSerializationBehavior>().ToArray();
        }

        public void Serialize(object[] messages, Stream messageStream)
        {
            if(messages.Length> MaxNumberOfAllowedItemsInCollection)
                throw new UnboundedResultSetException("A message batch is limited to 256 messages");

            var namespaces = GetNamespaces(messages);
            var messagesElement = new XElement(namespaces["esb"] + "messages");
            var xml = new XDocument(messagesElement);

            foreach (var m in messages)
            {
                if (m == null)
                    continue;

                try
                {
                    WriteObject(reflection.GetNameForXml(m.GetType()), m, messagesElement, namespaces);
                }
                catch (Exception e)
                {
                    throw new SerializationException("Could not serialize " + m.GetType() + ".", e);
                }
            }

            messagesElement.Add(
                namespaces.Select(x => new XAttribute(XNamespace.Xmlns + x.Key, x.Value))
                );

            var streamWriter = new StreamWriter(messageStream);
            var writer = XmlWriter.Create(streamWriter, new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8
            });
            if (writer == null)
                throw new InvalidOperationException("Could not create xml writer from stream");

            xml.WriteTo(writer);
            writer.Flush();
            streamWriter.Flush();
        }

        private void WriteObject(string name, object value, XContainer parent, IDictionary<string, XNamespace> namespaces)
        {
            var valueType = value != null ? value.GetType() : null;
            if(HaveCustomValueConvertor(valueType))
            {
                var valueConvertorType = reflection.GetGenericTypeOf(typeof (IValueConvertor<>), value);
                var convertor = serviceLocator.Resolve(valueConvertorType);

                var elementName = GetXmlNamespace(namespaces, valueType) + name;

                var convertedValue = reflection.InvokeToElement(convertor, value, v => GetXmlNamespace(namespaces, v));

            	convertedValue = ApplyMessageSerializationBehaviorIfNecessary(valueType, convertedValue);

                parent.Add(new XElement(elementName, convertedValue));
            }
			else if(HaveCustomSerializer(valueType))
			{
				var customSerializer = customElementSerializers.First(s => s.CanSerialize(valueType));
				var elementName = GetXmlNamespace(namespaces, valueType) + name;
				var element = customSerializer.ToElement(value, v => GetXmlNamespace(namespaces, v));
				var customElement = new XElement(elementName, element);
				customElement = ApplyMessageSerializationBehaviorIfNecessary(valueType, customElement);
				parent.Add(customElement);
            }
            else if (value == null)
            {
                var ns = GetXmlNamespace(namespaces,  null);
                var nil = new XAttribute(ns + "nil", true);
                parent.Add(new XElement(name, nil));
            }
            else if (ShouldPutAsString(value))
            {
                var elementName = GetXmlNamespace(namespaces, valueType) + name;
                parent.Add(new XElement(elementName, FormatAsString(value)));
            }
			else if (value is byte[])
			{
				var elementName = GetXmlNamespace(namespaces, typeof(byte[])) + name;
				parent.Add(new XElement(elementName, Convert.ToBase64String((byte[]) value)));
			}
			else if (ShouldTreatAsDictionary(valueType))
			{
				XElement list = GetContentWithNamespace(value, namespaces, name);
				parent.Add(list);
				var itemCount = 0;
				foreach (var item in ((IEnumerable)value))
				{
					if (item == null)
						continue;
					itemCount += 1;
					if (itemCount > MaxNumberOfAllowedItemsInCollection)
						throw new UnboundedResultSetException("You cannot send collections with more than 256 items (" + value + " " + name + ")");

					var entry = new XElement("entry");
					var keyProp = reflection.Get(item, "Key");
					if (keyProp == null)
						continue;
					WriteObject("Key", keyProp, entry, namespaces);
					var propVal = reflection.Get(item, "Value");
					if (propVal != null)
					{
						WriteObject("Value", propVal, entry, namespaces);
					}
					
					list.Add(entry);
				}
			}
			else if (value is IEnumerable)
            {
                XElement list = GetContentWithNamespace(value, namespaces, name);
                parent.Add(list);
                var itemCount = 0;
                foreach (var item in ((IEnumerable)value))
                {
                    itemCount += 1;
                    if (itemCount > MaxNumberOfAllowedItemsInCollection)
                        throw new UnboundedResultSetException("You cannot send collections with more than 256 items (" + value + " " + name + ")");

                    WriteObject("value", item, list, namespaces);
                }
            }
            else
            {
                XElement content = GetContentWithNamespace(value, namespaces, name);
                foreach (var property in reflection.GetProperties(value))
                {
                    var propVal = reflection.Get(value, property);
                    if (propVal == null)
                        continue;
                    WriteObject(property, propVal, content, namespaces);
                }
            	content = ApplyMessageSerializationBehaviorIfNecessary(valueType, content);
				parent.Add(content);
            }
        }

        private static bool ShouldTreatAsDictionary(Type type)
    	{
    		if (type.IsGenericType == false)
				return false;

    		var genericArguments = type.GetGenericArguments();
			if (genericArguments.Length != 2)
				return false;

    		var interfaceType = typeof (IDictionary<,>).MakeGenericType(genericArguments);
			if (interfaceType.IsAssignableFrom(type) == false)
				return false;

    		return true;

    	}

    	private XElement ApplyMessageSerializationBehaviorIfNecessary(Type messageType, XElement element)
		{
			foreach (var afterSerializedBehavior in elementSerializationBehaviors)
			{
				if (afterSerializedBehavior.ShouldApplyBehavior(messageType))
					return afterSerializedBehavior.ApplyElementBehavior(element);
			}
			return element;
		}

		private XElement ApplyMessageDeserializationBehaviorIfNecessary(Type messageType, XElement element)
		{
			foreach (var afterSerializedBehavior in elementSerializationBehaviors)
			{
				if (afterSerializedBehavior.ShouldApplyBehavior(messageType))
					return afterSerializedBehavior.RemoveElementBehavior(element);
			}
			return element;
		}

        private XNamespace GetXmlNamespace(IDictionary<string, XNamespace> namespaces, Type type)
        {
            var ns = reflection.GetNamespacePrefixForXml(type);
            XNamespace xmlNs;
            if (namespaces.TryGetValue(ns, out xmlNs) == false)
            {
                namespaces[ns] = xmlNs = reflection.GetNamespaceForXml(type);
            }
            return xmlNs;
        }

		private bool HaveCustomSerializer(Type type)
        {
            if (type == null)
                return false;

			return customElementSerializers
				.Any(s => s.CanSerialize(type));
		}

        private bool HaveCustomValueConvertor(Type type)
        {
            if (type == null)
                return false;
            bool? hasConvertor = null;
            typeHasConvertorCache.Read(
                reader =>
                {
                    bool val;
                    if (reader.TryGetValue(type, out val))
                        hasConvertor = val;
                });
            if (hasConvertor != null)
                return hasConvertor.Value;

            var convertorType = reflection.GetGenericTypeOf(typeof(IValueConvertor<>),type);
            var component = serviceLocator.CanResolve(convertorType);
            typeHasConvertorCache.Write(writer => writer.Add(type, component));
            return component;
        }

        private XElement GetContentWithNamespace(object value, IDictionary<string, XNamespace> namespaces, string name)
        {
            var type = value != null ? value.GetType() : null;
            var xmlNsAlias = reflection.GetNamespacePrefixForXml(type);
            XNamespace xmlNs;
            if (namespaces.TryGetValue(xmlNsAlias, out xmlNs) == false)
            {
               namespaces[xmlNsAlias] = xmlNs = reflection.GetNamespaceForXml(type);
            }

            return new XElement(xmlNs + name);
        }

        private static bool ShouldPutAsString(object value)
        {
            return value is ValueType || value is string || value is Uri;
        }

        public static object FromString(Type type, string value)
        {
            if (type == typeof(string))
                return value;

            if (type == typeof(Uri))
                return new Uri(value);

            if (type.IsPrimitive)
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (type == typeof(Guid))
                return new Guid(value);

            if (type == typeof(DateTime))
                return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

			if (type == typeof(DateTimeOffset))
				return XmlConvert.ToDateTimeOffset(value);

            if (type == typeof(TimeSpan))
                return XmlConvert.ToTimeSpan(value);

            if (type.IsEnum)
                return Enum.Parse(type, value);

            if (type == typeof(decimal))
                return decimal.Parse(value, CultureInfo.InvariantCulture);

            throw new SerializationException("Don't know how to deserialize type: " + type + " from '" + value + "'");
        }

        private static string FormatAsString(object value)
        {
            if (value == null)
                return string.Empty;
            if (value is bool)
                return value.ToString().ToLower();
            if (value is string)
                return value as string;
            if (value is Uri)
                return value.ToString();

            if (value is DateTime)
                return ((DateTime)value).ToString("o", CultureInfo.InvariantCulture);

			if(value is DateTimeOffset)
				return XmlConvert.ToString((DateTimeOffset)value);

            if (value is TimeSpan)
            {
                var ts = (TimeSpan)value;
                return string.Format("P0Y0M{0}DT{1}H{2}M{3}S", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
            }
            if (value is Guid)
                return ((Guid)value).ToString();

            if (value is decimal)
                return ((decimal) value).ToString(CultureInfo.InvariantCulture);

            if (value is double)
                return ((double)value).ToString(CultureInfo.InvariantCulture);

            if (value is float)
                return ((float)value).ToString(CultureInfo.InvariantCulture);

            return value.ToString();
        }

        private IDictionary<string, XNamespace> GetNamespaces(object[] mesages)
        {
            var namespaces = new Dictionary<string, XNamespace>
            {
                {"esb", "http://servicebus.hibernatingrhinos.com/2008/12/20/esb"},
            };
            foreach (var msg in mesages)
            {
                if (msg == null)
                    continue;
                var type = msg.GetType();
                namespaces[reflection.GetNamespacePrefixForXml(type)] = reflection.GetNamespaceForXml(type);
            }
            return namespaces;
        }

        public object[] Deserialize(Stream message)
        {
            var namespaces = GetNamespaces(new object[0]);
            var document = XDocument.Load(XmlReader.Create(message));
            if (document.Root == null)
                throw new SerializationException("document doesn't have root element");

            if (document.Root.Name != namespaces["esb"] + "messages")
                throw new SerializationException("message doesn't have root element named 'messages'");

            var msgs = new List<object>();
            foreach (var element in document.Root.Elements())
            {
                var type = reflection.GetTypeFromXmlNamespace(element.Name.NamespaceName);
                if (type==null)
                {
                    throw new SerializationException("Cannot find root message type: " + element.Name.Namespace);
                }
                var msg = ReadObject(type, element);
                msgs.Add(msg);
            }
            return msgs.ToArray();
        }

        private object ReadObject(Type type, XElement element)
        {
            if (type == null)
                throw new ArgumentNullException("type");

        	element = ApplyMessageDeserializationBehaviorIfNecessary(type, element);
            if (ShouldTreatAsNullValue(element))
            {
                return null;
            }
            if(HaveCustomValueConvertor(type))
            {
                var convertorType = reflection.GetGenericTypeOf(typeof(IValueConvertor<>),type);
                var convertor = serviceLocator.Resolve(convertorType);
                return reflection.InvokeFromElement(convertor, element);
            }
			if(HaveCustomSerializer(type))
			{
				var customSerializer = customElementSerializers.First(s => s.CanSerialize(type));
				return customSerializer.FromElement(type, element);
			}
			if(type == typeof(byte[]))
			{
				return Convert.FromBase64String(element.Value);
			}
            if (CanParseFromString(type))
            {
                return FromString(type, element.Value);
            }
			if(ShouldTreatAsDictionary(type))
			{
				return ReadDictionary(type, element);
			}
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return ReadList(type, element);
            }
            object instance = reflection.CreateInstance(type);
            foreach (var prop in element.Elements())
            {
                var property = prop;
                reflection.Set(instance,
                    prop.Name.LocalName,
                    typeFromProperty =>
                    {
                        var propType = reflection.GetTypeFromXmlNamespace(property.Name.NamespaceName);
                        return ReadObject(propType ?? typeFromProperty, property);
                    });
            }
            return instance;
        }

        private bool ShouldTreatAsNullValue(XElement element)
        {
            if (!element.IsEmpty)
                return false;
            XNamespace xsi = reflection.GetNamespaceForXml(null);
            var nil = element.Attribute(xsi + "nil");
            if (nil == null) 
                return false;
            bool nullable;
            return Boolean.TryParse(nil.Value, out nullable) 
                        && nullable;
        }

        private static bool CanParseFromString(Type type)
        {
            if (type.IsPrimitive)
                return true;

            if (type == typeof(string))
                return true;

            if (type == typeof(Uri))
                return true;

            if (type == typeof(DateTime))
                return true;

			if (type == typeof(DateTimeOffset))
				return true;

            if (type == typeof(TimeSpan))
                return true;

            if (type == typeof(Guid))
                return true;

            if (type.IsEnum)
                return true;

            if (type == typeof(decimal))
                return true;

            return false;
        }

        private object ReadList(Type type, XContainer element)
        {
            object instance;
            Type elementType;
            if (type.IsArray)
            {
                instance = reflection.CreateInstance(type, element.Elements().Count());
                elementType = type.GetElementType();
            }
            else
            {
                instance = reflection.CreateInstance(type);
                elementType = type.GetGenericArguments()[0];
            }
            int index = 0;
            var array = instance as Array;
            foreach (var value in element.Elements())
            {
                var itemType = reflection.GetTypeFromXmlNamespace(value.Name.NamespaceName);
                object o = ReadObject(itemType ?? elementType, value);
                if (array != null)
                    array.SetValue(o, index);
                else
                    reflection.InvokeAdd(instance, o);

                index += 1;
            }
            return instance;
        }

		private object ReadDictionary(Type type, XContainer element)
		{
			object instance = reflection.CreateInstance(type);
			var genericArguments = type.GetGenericArguments();
			var keyType = genericArguments[0];
			var valueType = genericArguments[1];
			foreach (var entry in element.Elements())
			{
				var elements = entry.Elements().ToArray();
				var itemKeyType = reflection.GetTypeFromXmlNamespace(elements[0].Name.NamespaceName);
				object key = ReadObject(itemKeyType ?? keyType, elements[0]);

				object value = null;
				if (elements.Length > 1)
				{
					var itemValueType = reflection.GetTypeFromXmlNamespace(elements[1].Name.NamespaceName);
					value = ReadObject(itemValueType ?? valueType, elements[1]);
				}

				reflection.InvokeAdd(instance, key, value);
			}
			return instance;
		}
    }
}
