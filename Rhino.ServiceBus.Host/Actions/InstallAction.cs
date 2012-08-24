using System;
using System.Collections;
using System.Configuration.Install;
using Microsoft.Win32;

namespace Rhino.ServiceBus.Host.Actions
{
    public class InstallAction : IAction
    {
        public void Execute(ExecutingOptions options)
        {
            var installer = new ProjectInstaller
            {
                DisplayName = options.Name,
                Description = options.Name,
                Context = new InstallContext()
            };

            if (!string.IsNullOrEmpty(options.Account))
            {
                if (string.IsNullOrEmpty(options.Password))
                {
                    throw new InvalidOperationException("When /Action is Install and /Account is set /Password is required.");
                }

                installer.SetUserAccount(options.Account, options.Password);
            }

			installer.Context.Parameters.Add("assemblypath", GetType().Assembly.Location);
			
			installer.Install(new Hashtable());

            using (var system = Registry.LocalMachine.OpenSubKey("System"))
            using (var currentControlSet = system.OpenSubKey("CurrentControlSet"))
            using (var services = currentControlSet.OpenSubKey("Services"))
            using (var service = services.OpenSubKey(installer.ServiceName, true))
            {
                var path = (string)service.GetValue("ImagePath");

				options.Action = Action.Server;

                service.SetValue("ImagePath", path + options);

                if (options.DependOnService != null && options.DependOnService.Length > 0)
                    service.SetValue("DependOnService", options.DependOnService, RegistryValueKind.MultiString);
                else
                    service.DeleteValue("DependOnService", false);
            }
        }
    }
}