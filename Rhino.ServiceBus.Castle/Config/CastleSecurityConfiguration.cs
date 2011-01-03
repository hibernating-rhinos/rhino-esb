using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Convertors;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Castle.Config
{
    public class CastleSecurityConfiguration : IBusConfigurationAware 
    {
        private readonly IWindsorContainer container;

        public CastleSecurityConfiguration(IWindsorContainer container)
        {
            this.container = container;
        }

        private void RegisterNoSecurity()
        {
            container.Register(
                   Component.For<IValueConvertor<WireEcryptedString>>()
                       .ImplementedBy<ThrowingWireEcryptedStringConvertor>()
                   );
            container.Register(
                Component.For<IElementSerializationBehavior>()
                    .ImplementedBy<ThrowingWireEncryptedMessageConvertor>()
                );
        }

        private void RegisterSecurity(byte[] key)
        {
            container.Register(
				Component.For<IEncryptionService>()
					.ImplementedBy<RijndaelEncryptionService>()
					.DependsOn(new
					{
						key,
					})
					.Named("esb.security")
				);

            container.Register(
                Component.For<IValueConvertor<WireEcryptedString>>()
                    .ImplementedBy<WireEcryptedStringConvertor>()
					.ServiceOverrides(ServiceOverride.ForKey("encryptionService").Eq("esb.security"))
                );

        	container.Register(
				Component.For<IElementSerializationBehavior>()
					.ImplementedBy<WireEncryptedMessageConvertor>()
					.ServiceOverrides(ServiceOverride.ForKey("encryptionService").Eq("esb.security"))
        		);
        }

        public void Configure(AbstractRhinoServiceBusFacility config)
        {
            var busConfig = config as RhinoServiceBusFacility;
            if (busConfig == null)
                return;

            var securityReader = new SecurityConfigurationReader(config);
            if (securityReader.Key == null)
                RegisterNoSecurity();
            else
                RegisterSecurity(securityReader.Key);
        }
    }
}