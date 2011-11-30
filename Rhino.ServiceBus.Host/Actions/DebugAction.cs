using System;

namespace Rhino.ServiceBus.Host.Actions
{
    public class DebugAction : IAction
    {
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
                Console.WriteLine(e);
                Console.ReadKey();
            }
            
        }
    }
}