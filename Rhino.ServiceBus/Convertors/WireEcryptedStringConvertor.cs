using System;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Convertors
{
    public class WireEcryptedStringConvertor : IValueConvertor<WireEcryptedString>
    {
        public byte[] Key{ get; set;}

        public WireEcryptedStringConvertor(byte[] key)
        {
            Key = key;
        }

        public XElement ToElement(WireEcryptedString val, Func<Type, XNamespace> getNamespace)
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
                    writer.Write(val);
                    writer.Flush();
                    cryptoStream.Flush();
                    cryptoStream.FlushFinalBlock();

                    return new XElement(getNamespace(typeof (string)) + "Value",
                                        new XAttribute("iv", Convert.ToBase64String(rijndael.IV)),
                                        Convert.ToBase64String(memoryStream.ToArray())
                        );
                }
            }
        }

        public WireEcryptedString FromElement(XElement element)
        {
            var value = element.Element(XName.Get("Value","string"));
            if(value==null)
                throw new ArgumentException("element must contain <value> element");
            
            var attribute = value.Attribute("iv");
            if(attribute==null)
                throw new ArgumentException("element must contain a <value> element with iv attribue");

            var base64String = Convert.FromBase64String(element.Value);

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = Key;
                rijndael.IV = Convert.FromBase64String(attribute.Value);
                rijndael.Mode = CipherMode.CBC;

                using (var decryptor = rijndael.CreateDecryptor())
                using (var memoryStream = new MemoryStream(base64String))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}