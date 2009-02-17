using System;
using System.Configuration;
using Castle.Core.Configuration;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Convertors;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Config
{
    public class SecurityConfiguration : IBusConfigurationAware
    {
        public void Configure(RhinoServiceBusFacility facility, IConfiguration configuration)
        {
            var kernel = facility.Kernel;

            var security = configuration.Children["security"];

            if (security == null)
            {
                kernel.Register(
                    Component.For<IValueConvertor<WireEcryptedString>>()
                        .ImplementedBy<ThrowingWireEcryptedStringConvertor>()
                    );
                return;
            }

            var key = security.Children["key"];
            if (key == null || string.IsNullOrEmpty(key.Value))
                throw new ConfigurationErrorsException("<security> element must have a <key> element with content");

            var keyBuffer = Convert.FromBase64String(key.Value);
            kernel.Register(
                Component.For<IValueConvertor<WireEcryptedString>>()
                    .ImplementedBy<WireEcryptedStringConvertor>()
                    .DependsOn(
                        Property.ForKey("key").Eq(keyBuffer)
                    )
                );
        }
    }
}