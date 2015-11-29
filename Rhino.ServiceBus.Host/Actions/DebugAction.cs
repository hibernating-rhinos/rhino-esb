using System;
using Common.Logging;

namespace Rhino.ServiceBus.Host.Actions
{
    public class DebugAction : IAction
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DebugAction));

        public void Execute(ExecutingOptions options)
        {
            var host = new RhinoServiceBusHost();
            host.SetArguments(options);
            try
            {
				host.DebugStart(new string[0]);
            	bool keepGoing = true;
				while (keepGoing)
            	{
					Console.WriteLine("Enter 'cls' to clear the screen, 'q' to exit");
            		var op = Console.ReadLine() ?? "";
            		switch (op.ToLowerInvariant())
            		{
						case "q":
            				keepGoing = false;
            				break;
						case "cls":
							Console.Clear();
            				break;
            		}

            	}
                host.Stop();
            }
            catch (Exception e)
            {
                Log.Fatal("Host has crashed", e);
                Console.WriteLine(e);
                Console.ReadKey();
            }
            
        }
    }
}