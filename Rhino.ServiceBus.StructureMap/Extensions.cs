using System;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.StructureMap;
using StructureMap;

namespace Rhino.ServiceBus
{
    [CLSCompliant(false)]
    public static class Extensions
    {
        public static AbstractRhinoServiceBusConfiguration UseStructureMap(this AbstractRhinoServiceBusConfiguration configuration)
        {
            return UseStructureMap(configuration, ObjectFactory.Container);
        }

        public static AbstractRhinoServiceBusConfiguration UseStructureMap(this AbstractRhinoServiceBusConfiguration configuration, IContainer container)
        {
            new StructureMapBuilder(configuration, container);
            return configuration;
        }
    }
}