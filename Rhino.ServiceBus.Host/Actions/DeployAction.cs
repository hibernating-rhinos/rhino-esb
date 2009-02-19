namespace Rhino.ServiceBus.Host.Actions
{
    public class DeployAction : IAction
    {
        public void Execute(ExecutingOptions options)
        {
            var host = new RhinoServiceBusHost();
            host.SetArguments(options);
            host.InitialDeployment(options.Account);
        }
    }
}