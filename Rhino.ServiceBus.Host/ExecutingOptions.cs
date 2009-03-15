using System.Text;
using CommandLine;

namespace Rhino.ServiceBus.Host
{
    public class ExecutingOptions
    {
        [Argument(ArgumentType.Required, HelpText = "Choose an action", ShortName = "action")] public Action Action;

        [Argument(ArgumentType.AtMostOnce, HelpText = "Assembly to execute", ShortName = "asm")] public string Assembly;

        [Argument(ArgumentType.AtMostOnce, HelpText = "Configuration file", ShortName = "config")] public string ConfigFile;

        [Argument(ArgumentType.Required, HelpText = "Service name", ShortName = "name")] public string Name;

        [Argument(ArgumentType.AtMostOnce, LongName = "Account")] public string Account;

        [Argument(ArgumentType.AtMostOnce, LongName = "Host")] public string Host;
        [Argument(ArgumentType.AtMostOnce, LongName = "BootStrapper")] public string BootStrapper;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(" /Action:").Append(Action)
                .Append(" /Name:\"")
                .Append(Name)
				.Append("\"");

            if(string.IsNullOrEmpty(Account)==false)
            {
                sb.Append(" /Account:")
                .Append(Account);
            }

            if (string.IsNullOrEmpty(Host)==false)
            {
                sb.Append(" \"/Host:")
                    .Append(Host)
                    .Append("\"");
            }

            if (string.IsNullOrEmpty(BootStrapper) == false)
            {
                sb.Append(" \"/BootStrapper:")
                    .Append(BootStrapper)
                    .Append("\"");
            }
                
            if (string.IsNullOrEmpty(Assembly) == false)
            {
                sb.Append(" /Assembly:\"")
                    .Append(Assembly)
					.Append("\"");
            }
            if (string.IsNullOrEmpty(ConfigFile) == false)
            {
                sb.Append(" /ConfigFile:\"")
                    .Append(ConfigFile)
					.Append("\"");
            }
            return sb.ToString();
        }
    }
}
