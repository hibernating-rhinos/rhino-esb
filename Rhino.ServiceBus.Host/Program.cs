using System.Collections.Generic;
using CommandLine;
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
            {Action.Uninstall, new UninstallAction()}
        };

        public static void Main(string[] args)
        {
            var executingOptions = new ExecutingOptions();
            if (Parser.ParseArguments(args, executingOptions) == false)
            {
                Console.WriteLine();
                Console.WriteLine(Parser.ArgumentsUsage(typeof(ExecutingOptions)));
                return;
            }

            try
            {
                actions[executingOptions.Action].Execute(executingOptions);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}