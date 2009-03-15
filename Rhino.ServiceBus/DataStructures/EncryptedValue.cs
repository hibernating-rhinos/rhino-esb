namespace Rhino.ServiceBus.DataStructures
{
	public class EncryptedValue
	{
		public string EncryptedBase64Value { get; set; }
		public string Base64Iv { get; set; }
	}
}
