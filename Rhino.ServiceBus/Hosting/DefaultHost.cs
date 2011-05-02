using System;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;
using Rhino.ServiceBus.Config;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Hosting
{
    public class DefaultHost : MarshalByRefObject, IApplicationHost
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(DefaultHost));
        private string assemblyName;
        private AbstractBootStrapper bootStrapper;
        private IStartable startable;
        private string bootStrapperName;
        private BusConfigurationSection hostConfiguration;

        public IStartable Bus
        {
            get { return startable; }
        }

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

            startable.Start();

            bootStrapper.AfterStart();
        }

        private void InitailizeBus(string asmName)
        {
            string logfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");

            XmlConfigurator.ConfigureAndWatch(new FileInfo(logfile));

            assemblyName = asmName;

            CreateBootStrapper();

            log4net.GlobalContext.Properties["BusName"] = bootStrapper.GetType().Namespace;

            InitializeContainer();

            bootStrapper.BeforeStart();

            logger.Debug("Starting bus");
            startable = bootStrapper.GetInstance<IStartable>();
        }

        private void InitializeContainer()
        {
            bootStrapper.InitializeContainer();
            if (hostConfiguration != null)
                bootStrapper.UseConfiguration(hostConfiguration);
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

        private static Type GetAutoBootStrapperType(Assembly assembly)
        {
            var bootStrappers = assembly.GetTypes()
                .Where(x => typeof(AbstractBootStrapper).IsAssignableFrom(x) && x.IsAbstract == false)
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
            if (startable != null)
                startable.Dispose();
        }

        public override object InitializeLifetimeService()
        {
            return null; //singleton
        }

        public void InitialDeployment(string asmName, string user)
        {
            InitailizeBus(asmName);
            bootStrapper.ExecuteDeploymentActions(user);
            
            bootStrapper.ExecuteEnvironmentValidationActions();
        }

        public void BusConfiguration(Func<HostConfiguration, HostConfiguration> configuration)
        {
            hostConfiguration = configuration(new HostConfiguration()).ToBusConfiguration();
        }
    }
}
