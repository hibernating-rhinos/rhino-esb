using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Config
{
    public interface IBusConfigurationAware
    {
        void Configure(AbstractRhinoServiceBusFacility config, IBusContainerBuilder builder);
    }
}