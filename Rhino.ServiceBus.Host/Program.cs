using System.Collections.Generic;
using CommandLine;
using log4net;
using Rhino.ServiceBus.Host.Actions;

namespace Rhino.ServiceBus.Host
{
    using System;

    public class Program
    {
        private static readonly IDictionary<Action, IAction> actions = new Dictionary<Action, IAction>
        {
            {Action.Debug, new DebugAction()},
            {Action.Server, new ServerAction()},
            {Action.Install, new InstallAction()},
            {Action.Uninstall, new UninstallAction()},
            {Action.Deploy, new DeployAction()}
        };

    	private static ILog log = LogManager.GetLogger(typeof (Program));

        public static int Main(string[] args)
        {
            var executingOptions = new ExecutingOptions();
            if (Parser.ParseArguments(args, executingOptions) == false)
            {
                Console.WriteLine("Invalid arguments:");
                Console.WriteLine("\t{0}",
                    string.Join(" ",args));
                Console.WriteLine();
                Console.WriteLine(Parser.ArgumentsUsage(typeof(ExecutingOptions)));

                return 1;
            }

            try
            {
                actions[executingOptions.Action].Execute(executingOptions);

                return 0;
            }
            catch (Exception e)
            {
                log.Fatal("Host has crashed because of an error",e);
				// want to put the error in the error log
				if(executingOptions.Action == Action.Server)
					throw;

                return 2;
            }
        }
    }
}