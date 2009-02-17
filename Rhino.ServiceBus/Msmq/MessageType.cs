namespace Rhino.ServiceBus.Msmq
{
	public enum MessageType
	{
		StandardMessage = 0,
		ShutDownMessageMarker = 3,
		AdministrativeMessageMarker = 4,
		TimeoutMessageMarker = 5,
	    LoadBalancerMessageMarker = 6,
        MoveMessageMarker = 7
	}
}
