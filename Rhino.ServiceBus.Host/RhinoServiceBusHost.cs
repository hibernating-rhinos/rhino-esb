
namespace Rhino.ServiceBus.Host
{
	using System.ServiceProcess;
	using Hosting;

	internal partial class RhinoServiceBusHost : ServiceBase
	{
		private RemoteAppDomainHost host;
		private string asm;
	    private string cfg;

	    public RhinoServiceBusHost()
		{
			InitializeComponent();
		}

		public void SetArguments(string assembly, string config)
		{
		    asm = assembly;
		    cfg = config;
		}

		protected override void OnStart(string[] ignored)
		{
            host = new RemoteAppDomainHost(asm, cfg);
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

	    public void CreateQueues()
	    {
	        var tmpHost = new RemoteAppDomainHost(asm, cfg);
	        tmpHost.CreateQueues();
            tmpHost.Close();
	        
	    }
	}
}