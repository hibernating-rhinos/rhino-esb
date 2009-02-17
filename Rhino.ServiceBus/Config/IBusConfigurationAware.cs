using Castle.Core.Configuration;
using Castle.MicroKernel;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public interface IBusConfigurationAware
    {
        void Configure(
            RhinoServiceBusFacility facility, 
            IConfiguration configuration
            );
    }
}