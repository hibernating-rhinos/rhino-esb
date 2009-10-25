namespace Rhino.ServiceBus
{
    public interface IOnewayBus
    {
        void Send(params object[] msgs);
    }
}