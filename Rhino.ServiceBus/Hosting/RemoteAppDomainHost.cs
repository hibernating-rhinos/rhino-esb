using System;
using System.Collections;
using System.IO;
using System.Threading;
using log4net;

namespace Rhino.ServiceBus.Hosting
{
    using System.Reflection;
    using System.Text;

    public class RemoteAppDomainHost
    {
        private readonly string boosterType;
        private readonly string assembly;
        private readonly string path;
        private HostedService current;
        private string configurationFile;
        private string hostType = typeof (DefaultHost).FullName;
        private string hostAsm = typeof (DefaultHost).Assembly.FullName;

        public RemoteAppDomainHost(Assembly asm, string configuration)
        {
            assembly = asm.Location;
            configurationFile = configuration;
        }

        public RemoteAppDomainHost(Type boosterType)
            : this(boosterType.Assembly.Location, null)
        {
            this.boosterType = boosterType.FullName;
        }

        public RemoteAppDomainHost(string assemblyPath, string config)
        {
            configurationFile = config;
            assembly = Path.GetFileNameWithoutExtension(assemblyPath);
            path = Path.GetDirectoryName(assemblyPath);
        }

        public void Start()
        {
            HostedService service = CreateNewAppDomain();
            current = service;
            try
            {
                service.Start();
            }
            catch (ReflectionTypeLoadException e)
            {
                var sb = new StringBuilder();
                foreach (var exception in e.LoaderExceptions)
                {
                    sb.AppendLine(exception.ToString());
                }
                throw new TypeLoadException(sb.ToString(), e);
            }
        }

        private HostedService CreateNewAppDomain()
        {
            var appDomainSetup = new AppDomainSetup
            {
                ApplicationBase = path,
                ApplicationName = assembly,
                ConfigurationFile = ConfigurationFile,
                ShadowCopyFiles = "true" //yuck
            };
            AppDomain appDomain = AppDomain.CreateDomain(assembly, null, appDomainSetup);
            return CreateRemoteHost(appDomain);
        }

        protected virtual HostedService CreateRemoteHost(AppDomain appDomain)
        {
            object instance = appDomain.CreateInstanceAndUnwrap(hostAsm,
                                                                hostType);
            var hoster = (IApplicationHost)instance;

            if (boosterType != null)
                hoster.SetBootStrapperTypeName(boosterType);

            return new HostedService(hoster, assembly, appDomain);
        }

        private string ConfigurationFile
        {
            get
            {
                if (configurationFile != null)
                    return configurationFile;
                configurationFile = Path.Combine(path, assembly + ".dll.config");
                if (File.Exists(configurationFile) == false)
                    configurationFile = Path.Combine(path, assembly + ".exe.config");
                return configurationFile;
            }
        }

        public void Close()
        {
            if (current != null)
                current.Stop();
        }

        #region Nested type: HostedService

        protected class HostedService
        {
            private readonly IApplicationHost hoster;
            private readonly string assembly;
            private readonly AppDomain appDomain;

            public HostedService(IApplicationHost hoster, string assembly, AppDomain appDomain)
            {
                this.hoster = hoster;
                this.assembly = assembly;
                this.appDomain = appDomain;
            }

            public void Stop()
            {
                hoster.Dispose();
                AppDomain.Unload(appDomain);
            }

            public void Start()
            {
                hoster.Start(assembly);
            }

            public void InitialDeployment(string user)
            {
                hoster.InitialDeployment(assembly, user);
            }
        }

        #endregion

        public void InitialDeployment(string user)
        {
            HostedService service = CreateNewAppDomain();
            service.InitialDeployment(user);
        }

        public RemoteAppDomainHost SetHostType(Type host)
        {
            hostType = host.FullName;
            hostAsm = host.Assembly.FullName;
            return this;
        }

        public void SetHostType(string hostTypeName)
        {
            var parts = hostTypeName.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length!=2)
                throw new InvalidOperationException("Could not parse host name");
            hostType = parts[0].Trim();
            hostAsm = parts[1].Trim();
        }

        public RemoteAppDomainHost Configuration(string configFile)
        {
            configurationFile = configFile;
            return this;
        }
    }
}