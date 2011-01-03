using System;
using System.Runtime.Remoting;
using Castle.DynamicProxy;

namespace Rhino.ServiceBus.Util
{
    //Pulled from Castle.Windsor 
    public static class ProxyUtil
    {
        public static Type GetUnproxiedType(object instance)
        {
            if (!RemotingServices.IsTransparentProxy(instance))
            {
                IProxyTargetAccessor accessor = instance as IProxyTargetAccessor;
                if (accessor != null)
                {
                    object target = accessor.DynProxyGetTarget();
                    if (target != null)
                    {
                        if (ReferenceEquals(target, instance))
                        {
                            return instance.GetType().BaseType;
                        }
                        instance = target;
                    }
                }
            }
            return instance.GetType();
        }

 

    }
}