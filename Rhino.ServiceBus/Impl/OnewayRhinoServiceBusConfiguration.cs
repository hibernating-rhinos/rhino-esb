namespace Rhino.ServiceBus.Impl
{
    public class OnewayRhinoServiceBusConfiguration : AbstractRhinoServiceBusConfiguration
    {
        protected override void ApplyConfiguration()
        {   
        }

        public MessageOwner[] MessageOwners { get; set; }
    }
}