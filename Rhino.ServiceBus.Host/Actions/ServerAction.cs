using System.ServiceProcess;

namespace Rhino.ServiceBus.Host.Actions
{
    public class ServerAction : IAction
    {
        public void Execute(ExecutingOptions options)
        {
            var host = new RhinoServiceBusHost();
            host.SetArguments(options.Assembly, options.ConfigFile);
            ServiceBase.Run(host);
        }
    }
}