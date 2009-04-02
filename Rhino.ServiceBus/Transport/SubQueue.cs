namespace Rhino.ServiceBus.Transport
{
    public enum SubQueue
    {
        Errors = 1,
        Timeout = 2,
        Subscriptions = 3,
        Discarded = 4,
        Workers = 5,
        Endpoints = 6
    }
}