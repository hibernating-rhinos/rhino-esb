
using System;

namespace Rhino.ServiceBus.Host
{
	using System.ServiceProcess;
	using Hosting;

	internal partial class RhinoServiceBusHost : ServiceBase
	{
		private RemoteAppDomainHost host;
		private string asm;
	    private string cfg;
	    private string hostType;

	    public RhinoServiceBusHost()
		{
			InitializeComponent();
		}

		public void SetArguments(ExecutingOptions options)
		{
		    asm = options.Assembly;
		    cfg = options.ConfigFile;
		    hostType = options.Host;
		}

		protected override void OnStart(string[] ignored)
		{
            host = new RemoteAppDomainHost(asm, cfg);
            if (string.IsNullOrEmpty(hostType) == false)
                host.SetHostType(hostType);
			host.Start();
		}

		protected override void OnStop()
		{
			if (host != null)
				host.Close();
		}

		public void DebugStart(string[] arguments)
		{
			OnStart(arguments);
		}

	    public void InitialDeployment(string user)
	    {
	        var tmpHost = new RemoteAppDomainHost(asm, cfg);
	        tmpHost.InitialDeployment(user);
            tmpHost.Close();
	        
	    }
	}
}