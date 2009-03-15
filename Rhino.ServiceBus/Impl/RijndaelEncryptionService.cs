using System;
using System.IO;
using System.Security.Cryptography;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Impl
{
	public class RijndaelEncryptionService : IEncryptionService
	{
		public byte[] Key { get; set;}

		public RijndaelEncryptionService(byte[] key)
		{
			Key = key;
		}

		public EncryptedValue Encrypt(string value)
		{
			using (var rijndael = new RijndaelManaged())
			{
				rijndael.Key = Key;
				rijndael.Mode = CipherMode.CBC;
				rijndael.GenerateIV();

				using (var encryptor = rijndael.CreateEncryptor())
				using (var memoryStream = new MemoryStream())
				using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
				using (var writer = new StreamWriter(cryptoStream))
				{
					writer.Write(value);
					writer.Flush();
					cryptoStream.Flush();
					cryptoStream.FlushFinalBlock();
					return new EncryptedValue
					{
                        EncryptedBase64Value = Convert.ToBase64String(memoryStream.ToArray()),
						Base64Iv = Convert.ToBase64String(rijndael.IV)
					};
				}
			}
		}

		public string Decrypt(EncryptedValue encryptedValue)
		{
			var encrypted = Convert.FromBase64String(encryptedValue.EncryptedBase64Value);
			using (var rijndael = new RijndaelManaged())
			{
				rijndael.Key = Key;
				rijndael.IV = Convert.FromBase64String(encryptedValue.Base64Iv);
				rijndael.Mode = CipherMode.CBC;

				using (var decryptor = rijndael.CreateDecryptor())
				using (var memoryStream = new MemoryStream(encrypted))
				using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
				using (var reader = new StreamReader(cryptoStream))
				{
					return reader.ReadToEnd();
				}
			}
		}
	}
}
