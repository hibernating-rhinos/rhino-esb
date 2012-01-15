using System;
using System.Runtime.Remoting;
using Castle.DynamicProxy;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Castle
{
    public class CastleReflection : DefaultReflection
    {
        public override Type GetUnproxiedType(object instance)
        {
            //Pulled from Castle.Windsor 
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