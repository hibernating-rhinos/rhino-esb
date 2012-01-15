using System;
using System.Runtime.Remoting;
using Rhino.ServiceBus.Impl;
using Spring.Aop.Framework;

namespace Rhino.ServiceBus.Spring
{
    public class SpringReflection : DefaultReflection
    {
        public override Type GetUnproxiedType(object instance)
        {
            if (!RemotingServices.IsTransparentProxy(instance))
            {
                return AopUtils.GetTargetType(instance);
            }
            return instance.GetType();
        }
    }
}