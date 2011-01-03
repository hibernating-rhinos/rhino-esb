using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.MessageModules;

namespace Rhino.ServiceBus.Castle.Config
{
    public class CastleLoggingConfiguration : IBusConfigurationAware 
    {
        private readonly IWindsorContainer container;

        public CastleLoggingConfiguration(IWindsorContainer container)
        {
            this.container = container;
        }

        public void Configure(AbstractRhinoServiceBusFacility config)
        {
            var busConfig = config as RhinoServiceBusFacility;
            if (busConfig == null)
                return;

            var logReader = new LoggingConfigurationReader(config);
            if (logReader.LogEndpoint == null)
                return;

            container.Register(
                Component.For<MessageLoggingModule>()
                    .DependsOn(new {logQueue = logReader.LogEndpoint})
                );
            config.InsertMessageModuleAtFirst<MessageLoggingModule>();
        }
    }
}