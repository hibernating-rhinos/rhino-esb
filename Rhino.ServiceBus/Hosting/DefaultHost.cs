using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core.Configuration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using log4net;
using log4net.Config;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Hosting
{
    public class DefaultHost : MarshalByRefObject, IApplicationHost
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(DefaultHost));
        private string assemblyName;
        private AbstractBootStrapper bootStrapper;
        private IWindsorContainer container;
        private IStartableServiceBus serviceBus;
        private string bootStrapperName;
    	private string standaloneCastleConfigurationFileName;
        private IConfiguration hostConfiguration;

        public void SetBootStrapperTypeName(string typeName)
        {
            bootStrapperName = typeName;
        }

        public void Start<TBootStrapper>()
            where TBootStrapper : AbstractBootStrapper
        {
            SetBootStrapperTypeName(typeof(TBootStrapper).FullName);
            Start(typeof(TBootStrapper).Assembly.FullName);
        }

        public void Start(string asmName)
        {
            InitailizeBus(asmName);

            serviceBus.Start();

            bootStrapper.AfterStart();
        }

        private void InitailizeBus(string asmName)
        {
            string logfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");

            XmlConfigurator.ConfigureAndWatch(new FileInfo(logfile));

            assemblyName = asmName;

            CreateBootStrapper();

            log4net.GlobalContext.Properties["BusName"] = bootStrapper.GetType().Namespace;

            CreateContainer();

            InitializeContainer();

            bootStrapper.BeforeStart();

            logger.Debug("Starting bus");
            serviceBus = container.Resolve<IStartableServiceBus>();
        }

        private void InitializeContainer()
        {
            bootStrapper.InitializeContainer(container);
        }

        private void CreateContainer()
        {
			if (container == null)
			{
				container = string.IsNullOrEmpty(standaloneCastleConfigurationFileName) 
					? File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile)
                        ? new WindsorContainer(new XmlInterpreter()) 
                        : new WindsorContainer() 
					: new WindsorContainer(new XmlInterpreter(standaloneCastleConfigurationFileName));
			}

            if(hostConfiguration != null)
                container.Kernel.ConfigurationStore.AddFacilityConfiguration("rhino.esb", hostConfiguration);

        	var facility = new RhinoServiceBusFacility();
            bootStrapper.ConfigureBusFacility(facility);
            container.Kernel.AddFacility("rhino.esb", facility);
        }

        private void CreateBootStrapper()
        {
            logger.DebugFormat("Loading {0}", assemblyName);
            var assembly = Assembly.Load(assemblyName);

            Type bootStrapperType = null;

            if (string.IsNullOrEmpty(bootStrapperName) == false)
                bootStrapperType = assembly.GetType(bootStrapperName);

            bootStrapperType = bootStrapperType ??
                GetAutoBootStrapperType(assembly);
            try
            {
                bootStrapper = (AbstractBootStrapper)Activator.CreateInstance(bootStrapperType);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to create " + bootStrapperType + ".", e);
            }
        }

        public IWindsorContainer Container
        {
            get { return container; }
        }

        private static Type GetAutoBootStrapperType(Assembly assembly)
        {
            var bootStrappers = assembly.GetTypes()
                .Where(x => typeof(AbstractBootStrapper).IsAssignableFrom(x))
                .ToArray();

            if (bootStrappers.Length == 0)
                throw new InvalidOperationException("Could not find a boot strapper for " + assembly);

            if (bootStrappers.Length > 1)
            {

                throw new InvalidOperationException("Found more than one boot strapper for " + assembly +
                    " you need to specify which boot strapper to use: " + Environment.NewLine +
                    string.Join(Environment.NewLine, bootStrappers.Select(x => x.FullName).ToArray()));
            }

            return bootStrappers[0];
        }

        public void Dispose()
        {
            if (bootStrapper != null)
                bootStrapper.Dispose();
            if (serviceBus != null)
                serviceBus.Dispose();
            if (container != null)
                container.Dispose();
        }

        public override object InitializeLifetimeService()
        {
            return null; //singleton
        }

        public void InitialDeployment(string asmName, string user)
        {
            InitailizeBus(asmName);

            foreach (var action in container.ResolveAll<IDeploymentAction>())
            {
                action.Execute(user);
            }

            foreach (var action in container.ResolveAll<IEnvironmentValidationAction>())
            {
                action.Execute();
            }
        }

		public void UseContainer(IWindsorContainer theContainer)
		{
			container = theContainer;
		}

		public void UseStandaloneCastleConfigurationFileName(string configurationFileName)
		{
			standaloneCastleConfigurationFileName = configurationFileName;
		}

        public void BusConfiguration(Func<HostConfiguration, HostConfiguration> configuration)
        {
            hostConfiguration = configuration(new HostConfiguration()).ToIConfiguration();
        }
    }
}
