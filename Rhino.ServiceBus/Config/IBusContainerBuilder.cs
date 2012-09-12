using System;
using System.Collections.Generic;
using System.Reflection;

namespace Rhino.ServiceBus.Config
{
    public interface IBusContainerBuilder
    {
        void WithInterceptor(IConsumerInterceptor interceptor);
        void RegisterDefaultServices(IEnumerable<Assembly> assemblies);
        void RegisterBus();
        void RegisterPrimaryLoadBalancer();
        void RegisterSecondaryLoadBalancer();
        void RegisterReadyForWork();
        void RegisterLoadBalancerEndpoint(Uri loadBalancerEndpoint);
        void RegisterLoggingEndpoint(Uri logEndpoint);
        void RegisterSecurity(byte[] key);
        void RegisterNoSecurity();
        void RegisterSingleton<T>(Func<T> func)
            where T : class;
        void RegisterSingleton<T>(string name, Func<T> func)
            where T : class;
        void RegisterAll<T>(params Type[] excludes)
            where T : class;
        void RegisterAll<T>(Predicate<Type> condition)
            where T : class;
    }
}