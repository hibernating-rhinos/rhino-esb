﻿using System.Collections.Generic;
using System.IO;
using CommandLine;
using Rhino.ServiceBus.Host.Actions;
using Rhino.ServiceBus.Impl;
using ILog = Rhino.ServiceBus.Internal.ILog;

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

        private static ILog log;

        private static ILog GetLogger(Type typ)
        {
            return new Log4NetWrapper(typ);
        }

        public static int Main(string[] args)
        {
            LogManager.Initialize(GetLogger);
            log = LogManager.GetLogger(typeof (Program));

            var executingOptions = new ExecutingOptions();
            if (Parser.ParseArguments(args, executingOptions) == false)
            {
                Console.WriteLine("Invalid arguments:");
                Console.WriteLine("\t{0}", string.Join(" ",args));
                Console.WriteLine();
                Console.WriteLine(Parser.ArgumentsUsage(typeof(ExecutingOptions)));
                return 1;
            }

        	var action = executingOptions.Action == Action.None
        	             	? (Environment.UserInteractive ? Action.Debug : Action.Server)
        	             	: executingOptions.Action;

        	executingOptions.Name = executingOptions.Name ?? Path.GetFileNameWithoutExtension(executingOptions.Assembly);

        	try
            {
                actions[action].Execute(executingOptions);

                return 0;
            }
            catch (Exception e)
            {
                log.Fatal("Host has crashed because of an error",e);
				// want to put the error in the error log
				if(action == Action.Server)
					throw;

                return 2;
            }
        }
    }
}