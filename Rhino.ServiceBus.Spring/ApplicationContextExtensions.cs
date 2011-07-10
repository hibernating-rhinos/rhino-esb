using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Spring.Context;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;

namespace Rhino.ServiceBus.Spring
{
    [CLSCompliant(false)]
    public static class ApplicationContextExtensions
    {
        public static T Get<T>(this IConfigurableApplicationContext context)
        {
            return (T) Get(context, typeof(T));
        }

        public static IEnumerable<T> GetAll<T>(this IConfigurableApplicationContext context)
        {
            IDictionary objectsOfType = context.GetObjectsOfType(typeof(T));
            return objectsOfType.Values.Cast<T>();
        }

        public static object Get(this IApplicationContext context, Type type)
        {
            string[] objectNamesForType = context.GetObjectNamesForType(type);
            if ((objectNamesForType == null) || (objectNamesForType.Length == 0))
            {
                throw new NoSuchObjectDefinitionException(type.FullName, "Requested Type not defined in the context.");
            }
            return context.GetObject(objectNamesForType[0]);
        }

        public static T Get<T>(this IApplicationContext context, string name)
        {
            return (T) context.GetObject(name);
        }

        public static void RegisterPrototype<T>(this IConfigurableApplicationContext context)
        {
            ObjectDefinitionBuilder definitionBuilder = ObjectDefinitionBuilder.RootObjectDefinition(new DefaultObjectDefinitionFactory(), typeof(T))
                .SetAutowireMode(AutoWiringMode.AutoDetect)
                .SetSingleton(false);
            context.ObjectFactory.RegisterObjectDefinition(Guid.NewGuid().ToString(), definitionBuilder.ObjectDefinition);
        }

        public static void RegisterSingletons<TBasedOn>(this IConfigurableApplicationContext context, Assembly assembly)
        {
            assembly.GetTypes()
                .Where(t => typeof (TBasedOn).IsAssignableFrom(t)
                            && !t.IsInterface
                            && !t.IsAbstract)
                .ToList()
                .ForEach(type => RegisterSingleton(context, type));
        }

        public static void RegisterSingleton(this IConfigurableApplicationContext context, Type type)
        {
            ObjectDefinitionBuilder definitionBuilder = ObjectDefinitionBuilder.RootObjectDefinition(new DefaultObjectDefinitionFactory(), type)
                .SetAutowireMode(AutoWiringMode.AutoDetect)
                .SetSingleton(true);
            context.ObjectFactory.RegisterObjectDefinition(type.FullName, definitionBuilder.ObjectDefinition);
        }

        public static void RegisterSingleton(this IConfigurableApplicationContext context, Type type, string name, params object[] constructorArguments)
        {
            ObjectDefinitionBuilder definitionBuilder = ObjectDefinitionBuilder.RootObjectDefinition(new DefaultObjectDefinitionFactory(), type)
                .SetAutowireMode(AutoWiringMode.AutoDetect)
                .SetSingleton(true);
            if (constructorArguments != null && constructorArguments.Length > 0)
            {
                foreach (object argument in constructorArguments)
                {
                    definitionBuilder.AddConstructorArg(argument);
                }
            }

            context.ObjectFactory.RegisterObjectDefinition(name, definitionBuilder.ObjectDefinition);
        }

        public static void RegisterSingleton<T>(this IConfigurableApplicationContext context, Func<T> creator)
        {
            RegisterSingleton(context, Guid.NewGuid().ToString(), creator);
        }

        public static void RegisterSingleton<T>(this IConfigurableApplicationContext context, string name, Func<T> creator)
        {
            context.ObjectFactory.RegisterSingleton(name, new FuncBasedObjectCreator<T>(creator));
        }

        private class FuncBasedObjectCreator<T> : IFactoryObject
        {
            private readonly Func<T> creator;

            public FuncBasedObjectCreator(Func<T> creator)
            {
                this.creator = creator;
            }

            public object GetObject()
            {
                return creator();
            }

            public Type ObjectType
            {
                get { return typeof(T); }
            }

            public bool IsSingleton
            {
                get { return true; }
            }
        }
    }
}