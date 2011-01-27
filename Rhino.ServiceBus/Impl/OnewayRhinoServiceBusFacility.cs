namespace Rhino.ServiceBus.Impl
{
    public class OnewayRhinoServiceBusFacility : AbstractRhinoServiceBusFacility
    {
        protected override void ApplyConfiguration()
        {   
        }

        public MessageOwner[] MessageOwners { get; set; }
    }
}