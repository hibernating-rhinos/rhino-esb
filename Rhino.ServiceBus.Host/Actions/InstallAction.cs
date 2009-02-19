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
            }
        }
    }
}