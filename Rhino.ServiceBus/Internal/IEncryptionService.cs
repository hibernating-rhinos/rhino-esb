using Rhino.ServiceBus.DataStructures;
namespace Rhino.ServiceBus.Internal
{
	public interface IEncryptionService
	{
		byte[] Key { get; set; }
		EncryptedValue Encrypt(string value);
		string Decrypt(EncryptedValue encryptedValue);
	}
}
