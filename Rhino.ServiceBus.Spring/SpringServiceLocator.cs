using System;
using System.Collections;
using System.Collections.Generic;

using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Spring;

using Spring.Context;
using Spring.Objects.Factory.Config;

namespace Rhino.ServiceBus
{
    [CLSCompliant(false)]
    public class SpringServiceLocator : IServiceLocator
    {
        private readonly IConfigurableApplicationContext applicationContext;

        public SpringServiceLocator(IConfigurableApplicationContext applicationContext)
        {
            this.applicationContext = applicationContext;
        }

        public T Resolve<T>()
        {
            return applicationContext.Get<T>();
        }

        public object Resolve(Type type)
        {
            return applicationContext.Get(type);
        }

        public bool CanResolve(Type type)
        {
            return applicationContext.GetObjectsOfType(type).Count > 0;
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            return applicationContext.GetAll<T>();
        }

        public IEnumerable<IHandler> GetAllHandlersFor(Type type)
        {
            IDictionary objectsOfType = applicationContext.GetObjectsOfType(type);
            List<IHandler> handlers = new List<IHandler>();
            foreach (DictionaryEntry dictionaryEntry in objectsOfType)
            {
                string objectName = (string) dictionaryEntry.Key;
                IObjectDefinition objectDefinition = applicationContext.ObjectFactory.GetObjectDefinition(objectName);
                handlers.Add(new DefaultHandler(objectDefinition.ObjectType, objectDefinition.ObjectType, () => applicationContext.GetObject(objectName)));
            }

            return handlers;
        }

        public void Release(object item)
        {
            // no need for spring
        }
    }
}