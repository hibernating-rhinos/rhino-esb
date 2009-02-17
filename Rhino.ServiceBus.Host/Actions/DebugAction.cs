using System;

namespace Rhino.ServiceBus.Host.Actions
{
    public class DebugAction : IAction
    {
        public void Execute(ExecutingOptions options)
        {
            var host = new RhinoServiceBusHost();
            host.SetArguments(options.Assembly, options.ConfigFile);
            host.DebugStart(new string[0]);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            host.Stop();
        }
    }
}