namespace Rhino.ServiceBus.Host.Actions
{
    public interface IAction
    {
        void Execute(ExecutingOptions options);
    }
}