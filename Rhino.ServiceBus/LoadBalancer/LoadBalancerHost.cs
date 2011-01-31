using System;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net.Config;
using Rhino.ServiceBus.Hosting;

namespace Rhino.ServiceBus.LoadBalancer
{
    public class LoadBalancerHost : MarshalByRefObject, IApplicationHost
    {
        private AbstractBootStrapper bootStrapper;
        private MsmqLoadBalancer loadBalancer;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Start(string assembly)
        {
            string logfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");

            XmlConfigurator.ConfigureAndWatch(new FileInfo(logfile));

            InitializeLoadBalancer(assembly);

            loadBalancer = bootStrapper.GetInstance<MsmqLoadBalancer>();
            log4net.GlobalContext.Properties["BusName"] = loadBalancer.Endpoint.Uri.AbsolutePath;
            loadBalancer.Start();
            bootStrapper.AfterStart();
        }

        private void InitializeLoadBalancer(string assemblyName)
        {
            CreateBootStrapper(assemblyName);
            bootStrapper.InitializeContainer();
            bootStrapper.BeforeStart();
        }

        public void InitialDeployment(string assembly, string user)
        {
            bootStrapper.ExecuteDeploymentActions(user);
            bootStrapper.ExecuteEnvironmentValidationActions();
        }

        public void SetBootStrapperTypeName(string type)
        {
        }

        private void CreateBootStrapper(string assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);

            Type bootStrapperType = GetAutoBootStrapperType(assembly);
            try
            {
                bootStrapper = (AbstractBootStrapper)Activator.CreateInstance(bootStrapperType);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to create " + bootStrapperType + ".", e);
            }
        }

        public void Dispose()
        {
            if (loadBalancer != null)
                loadBalancer.Dispose();
        }

        private static Type GetAutoBootStrapperType(Assembly assembly)
        {
            var bootStrappers = assembly.GetTypes()
                .Where(x => typeof(AbstractBootStrapper).IsAssignableFrom(x) && x.IsAbstract == false)
                .ToArray();

            if (bootStrappers.Length == 0)
                throw new InvalidOperationException("Could not find a load balancer boot strapper for " + assembly);

            if (bootStrappers.Length > 1)
            {

                throw new InvalidOperationException("Found more than one load balancer boot strapper for " + assembly +
                                                    " you need to specify which boot strapper to use: " + Environment.NewLine +
                                                    string.Join(Environment.NewLine, bootStrappers.Select(x => x.FullName).ToArray()));
            }

            return bootStrappers[0];
        }
    }
}