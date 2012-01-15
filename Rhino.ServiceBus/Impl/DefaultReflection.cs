using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using log4net;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Impl
{
    public class DefaultReflection : IReflection
    {
        private readonly ILog logger = LogManager.GetLogger(typeof (DefaultReflection));

        private readonly IDictionary<Type, string> typeToWellKnownTypeName;
        private readonly IDictionary<string, Type> wellKnownTypeNameToType;
        private readonly MethodInfo internalPreserveStackTraceMethod;


        public DefaultReflection()
        {
            internalPreserveStackTraceMethod = typeof(Exception).GetMethod("InternalPreserveStackTrace",
                                                                           BindingFlags.Instance | BindingFlags.NonPublic);


            wellKnownTypeNameToType = new Dictionary<string, Type>();
            typeToWellKnownTypeName = new Dictionary<Type, string>
            {
                {typeof (string), typeof (string).FullName},
                {typeof (int), typeof (int).FullName},
                {typeof (byte), typeof (byte).FullName},
                {typeof (bool), typeof (bool).FullName},
                {typeof (DateTime), typeof (DateTime).FullName},
                {typeof (TimeSpan), typeof (TimeSpan).FullName},
                {typeof (decimal), typeof (decimal).FullName},
                {typeof (float), typeof (float).FullName},
                {typeof (double), typeof (double).FullName},
                {typeof (char), typeof (char).FullName},
                {typeof (Guid), typeof (Guid).FullName},
                {typeof (Uri), typeof (Uri).FullName},
                {typeof (short), typeof (short).FullName},
                {typeof (long), typeof (long).FullName},
				{typeof(byte[]), "binary"}
            };
            foreach (var pair in typeToWellKnownTypeName)
            {
                wellKnownTypeNameToType.Add(pair.Value, pair.Key);
            }
        }

        #region IReflection Members

        public object CreateInstance(Type type, params object[] args)
        {
            try
            {
                return Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, args, null);
            }
            catch (Exception e)
            {
                throw new MissingMethodException("No parameterless constructor defined for this object: " + type, e);
            }
        }

        public Type GetTypeFromXmlNamespace(string xmlNamespace)
        {
            Type value;
            if (wellKnownTypeNameToType.TryGetValue(xmlNamespace, out value))
                return value;
            if(xmlNamespace.StartsWith("array_of_"))
            {
                return GetTypeFromXmlNamespace(xmlNamespace.Substring("array_of_".Length));
            }
            return Type.GetType(xmlNamespace);
        }

        public void InvokeAdd(object instance, object item)
        {
            try
            {
                Type type = instance.GetType();
                MethodInfo method = type.GetMethod("Add", new[] {item.GetType()});
                method.Invoke(instance, new[] {item});
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }

		public void InvokeAdd(object instance, object key, object value)
		{
			try
			{
				Type type = instance.GetType();
				type.InvokeMember("Add", BindingFlags.Public| BindingFlags.InvokeMethod | BindingFlags.Instance, null, instance, new[] {key, value});
			}
			catch (TargetInvocationException e)
			{
				throw InnerExceptionWhilePreservingStackTrace(e);
			}
		}

        public XElement InvokeToElement(object instance, object item, Func<Type, XNamespace> getNamespace)
        {
            try
            {
                Type type = instance.GetType();
                MethodInfo method = type.GetMethod("ToElement", new[] { item.GetType(), typeof(Func<Type, XNamespace>) });
                return (XElement)method.Invoke(instance, new [] { item, getNamespace });
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }

        public object InvokeFromElement(object instance, XElement value)
        {
            try
            {
                Type type = instance.GetType();
                MethodInfo method = type.GetMethod("FromElement", new[] { typeof(XElement) });
                return method.Invoke(instance, new [] { value });
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }


        public void Set(object instance, string name, Func<Type, object> generateValue)
        {
            try
            {
                Type type = instance.GetType();
                PropertyInfo property = type.GetProperty(name);
                if (property == null || property.CanWrite == false)
                {
                    logger.DebugFormat("Could not find settable property {0} to set on {1}", name, type);
                    return;
                }
                object value = generateValue(property.PropertyType);
                property.SetValue(instance, value, null);
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }

        private Exception InnerExceptionWhilePreservingStackTrace(TargetInvocationException e)
        {
            internalPreserveStackTraceMethod.Invoke(e.InnerException, new object[0]);
            return e.InnerException;
        }

        public object Get(object instance, string name)
        {
            try
            {
                Type type = instance.GetType();
                PropertyInfo property = type.GetProperty(name);
                if (property == null)
                {
                    logger.InfoFormat("Could not find property {0} to get on {1}", name, type);
                    return null;
                }
                return property.GetValue(instance, null);
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }

        public Type GetGenericTypeOf(Type type, object msg)
        {
            return GetGenericTypeOf(type, GetUnproxiedType(msg));
        }

        public Type GetGenericTypeOf(Type type, Type paramType)
        {
            return type.MakeGenericType(paramType);
        }

    	public Type GetGenericTypeOf(Type type, params Type[] paramTypes)
    	{
    		return type.MakeGenericType(paramTypes);
    	}

    	public ICollection<Type> GetGenericTypesOfWithBaseTypes(Type type, object msg)
    	{
			return GetGenericTypesOfWithBaseTypes(type, GetUnproxiedType(msg));
    	}

		public ICollection<Type> GetGenericTypesOfWithBaseTypes(Type type, Type paramType)
    	{
    		var constructedTypes = new List<Type>();

			//loop through all interfaces of the paramType, constructing a generic type for each.
			foreach (var interfaceType in paramType.GetInterfaces())
			{
				var constructedTypeWithInterfaceArg = GetGenericTypeOf(type, interfaceType);
				constructedTypes.Add(constructedTypeWithInterfaceArg);
			}

			//travel up the chain of base types, constructing a generic type for each.
    		Type currentParamType = paramType;
			while (currentParamType != null)
			{
				var constructedType = GetGenericTypeOf(type, currentParamType);
				constructedTypes.Add(constructedType);
				currentParamType = currentParamType.BaseType;
			}

    		return constructedTypes;
    	}

    	public void InvokeConsume(object consumer, object msg)
        {
            try
            {
                Type type = consumer.GetType();
                MethodInfo consume = type.GetMethod("Consume", new[] { msg.GetType() });
                consume.Invoke(consumer, new[] { msg });
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }

        public object InvokeSagaPersisterGet(object persister, Guid correlationId)
        {
            try
            {
                Type type = persister.GetType();
                MethodInfo method = type.GetMethod("Get");
                return method.Invoke(persister, new object[] {correlationId});
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }

        public void InvokeSagaPersisterSave(object persister, object entity)
        {
            try
            {
				
                Type type = persister.GetType();
                MethodInfo method = type.GetMethod("Save");
                method.Invoke(persister, new object[] {entity});
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }

        public void InvokeSagaPersisterComplete(object persister, object entity)
        {
            try
            {
                Type type = persister.GetType();
                MethodInfo method = type.GetMethod("Complete");
                method.Invoke(persister, new object[] {entity});
            }
            catch (TargetInvocationException e)
            {
                throw InnerExceptionWhilePreservingStackTrace(e);
            }
        }

    	public object InvokeSagaFinderFindBy(object sagaFinder, object msg)
    	{
			try
			{
				Type type = sagaFinder.GetType();
                MethodInfo method = type.GetMethod("FindBy", new[] { msg.GetType()} );
				return method.Invoke(sagaFinder, new object[] { msg });
			}
			catch (TargetInvocationException e)
			{
				throw InnerExceptionWhilePreservingStackTrace(e);
			}
    	}

    	public string GetNameForXml(Type type)
        {
            var typeName = type.Name;
        	typeName = typeName.Replace('[', '_').Replace(']', '_');
            var indexOf = typeName.IndexOf('`');
            if (indexOf == -1)
                return typeName;
            typeName = typeName.Substring(0, indexOf) + "_of_";
            foreach (var argument in type.GetGenericArguments())
            {
                typeName += GetNamespacePrefixForXml(argument) + "_";
            }
            return typeName.Substring(0, typeName.Length - 1);
        }

        public string GetNamespacePrefixForXml(Type type)
        {
            string value;
            if(typeToWellKnownTypeName.TryGetValue(type, out value))
                return value;
            if (type.IsArray)
                return "array_of_" + GetNamespacePrefixForXml(type.GetElementType());

            if (type.Namespace == null && type.Name.StartsWith("<>"))
                throw new InvalidOperationException("Anonymous types are not supported");

            if (type.Namespace == null) //global types?
            {
                return type.Name
                    .ToLowerInvariant();
            }
            var typeName = type.Namespace.Split('.')
                          .Last().ToLowerInvariant() + "." + type.Name.ToLowerInvariant();
            var indexOf = typeName.IndexOf('`');
            if (indexOf == -1)
                return typeName;
            typeName = typeName.Substring(0, indexOf)+ "_of_";
            foreach (var argument in type.GetGenericArguments())
            {
                typeName += GetNamespacePrefixForXml(argument) + "_";
            }
            return typeName.Substring(0,typeName.Length-1);
        }


        public string GetNamespaceForXml(Type type)
        {
            string value;
            if (typeToWellKnownTypeName.TryGetValue(type, out value))
                return value;

            Assembly assembly = type.Assembly;
            string fullName = assembly.FullName ?? assembly.GetName().Name;
            if (type.IsGenericType)
            {
                var builder = new StringBuilder();
                int startOfGenericName = type.FullName.IndexOf('[');
                builder.Append(type.FullName.Substring(0, startOfGenericName))
                    .Append("[")
                    .Append(String.Join(",",
                                    type.GetGenericArguments()
                                        .Select(t => "[" + GetNamespaceForXml(t) + "]")
                                        .ToArray()))
                    .Append("], ");
                if (assembly.GlobalAssemblyCache)
                {
                    builder.Append(fullName);
                }
                else
                {
                    builder.Append(fullName.Split(',')[0]);
                }
                return builder.ToString();
            }

            if (assembly.GlobalAssemblyCache == false)
            {
                return type.FullName + ", " + fullName.Split(',')[0];
            }
            return type.AssemblyQualifiedName;
        }

        public IEnumerable<string> GetProperties(object value)
        {
            return value.GetType().GetProperties()
                .Select(x => x.Name);
        }

        public Type[] GetMessagesConsumed(IMessageConsumer consumer)
        {
            Type consumerType = consumer.GetType();
            return GetMessagesConsumed(consumerType, type => false);
        }

        public Type[] GetMessagesConsumed(Type consumerType, Predicate<Type> filter)
        {
            var list = new HashSet<Type>();
            var toRemove = new HashSet<Type>();

            Type[] interfaces = consumerType.GetInterfaces();

            foreach (Type type in interfaces)
            {
                if (type.IsGenericType == false)
                    continue;

				if(type.GetGenericArguments()[0].IsGenericParameter)
					continue;

                Type definition = type.GetGenericTypeDefinition();

                if (filter(definition))
                {
                    toRemove.Add(type.GetGenericArguments()[0]);
                    continue;
                }

                if (definition != typeof (ConsumerOf<>))
                    continue;

                list.Add(type.GetGenericArguments()[0]);
            }
            list.ExceptWith(toRemove);
            return list.ToArray();
        }

        public virtual Type GetUnproxiedType(object instance)
        {
            // default to not understanding proxies
            return instance.GetType();
        }

        #endregion
    }
}
