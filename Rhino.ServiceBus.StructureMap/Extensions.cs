using Rhino.ServiceBus.Impl;
using StructureMap;

namespace Rhino.ServiceBus.StructureMap
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