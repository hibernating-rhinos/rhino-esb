using System.Collections;
using System.Configuration.Install;
using Microsoft.Win32;

namespace Rhino.ServiceBus.Host.Actions
{
    public class UninstallAction : IAction
    {
        public void Execute(ExecutingOptions options)
        {

            var installer = new ProjectInstaller
            {
                DisplayName = options.Name,
                Description = options.Name,
                Context = new InstallContext()
            };
            installer.Uninstall(null);
        }
    }
}