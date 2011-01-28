using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.StructureMap;
using StructureMap;

namespace Rhino.ServiceBus
{
    public static class Extensions
    {
        public static AbstractRhinoServiceBusFacility UseStructureMap(this AbstractRhinoServiceBusFacility configuration)
        {
            return UseStructureMap(configuration, ObjectFactory.Container);
        }

        public static AbstractRhinoServiceBusFacility UseStructureMap(this AbstractRhinoServiceBusFacility configuration, IContainer container)
        {
            new StructureMapBuilder(configuration, container);
            return configuration;
        }
    }
}