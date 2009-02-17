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
	    private readonly Type boosterType;
        private readonly string assembly;
        private string path;
        private HostedService current;
        private string configurationFile;

        public RemoteAppDomainHost Configuration(string configFile)
        {
            configurationFile = configFile;
            return this;
        }

        public RemoteAppDomainHost(Type boosterType)
            :this(boosterType.Assembly.Location, null)
        {
            this.boosterType = boosterType;
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
	        object instance = appDomain.CreateInstanceAndUnwrap("Rhino.ServiceBus",
	                                                            "Rhino.ServiceBus.Hosting.DefaultHost");
	        var hoster = (DefaultHost) instance;
            
	        if (boosterType != null)
	            hoster.SetBootStrapperTypeName(boosterType.FullName);

	        return new HostedService
	        {
                CreateQueues = ()=>hoster.CreateQueues(assembly),
	            Stop = ()=>
	            {
	                hoster.Dispose();
	                AppDomain.Unload(appDomain);
	            },
	            Start = () => hoster.Start(assembly)
	        };
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
            public Action Start;
            public Action Stop;
	        public Action CreateQueues;
        }

        #endregion

        public void CreateQueues()
        {
            HostedService service = CreateNewAppDomain();
            service.CreateQueues();
        }
    }
}