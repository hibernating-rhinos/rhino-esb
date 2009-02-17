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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(" /Action:").Append(Action)
                .Append(" /Name:")
                .Append(Name);

            if (string.IsNullOrEmpty(Assembly) == false)
            {
                sb.Append(" /Assembly:")
                    .Append(Assembly);
            }
            if (string.IsNullOrEmpty(ConfigFile) == false)
            {
                sb.Append(" /ConfigFile:")
                    .Append(ConfigFile);
            }
            return sb.ToString();
        }
    }
}
