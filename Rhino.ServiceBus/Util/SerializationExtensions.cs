using System.Collections.Specialized;
using System.IO;

namespace Rhino.ServiceBus.Util
{
    public static class SerializationExtensions
    {
        public static byte[] SerializeHeaders(this NameValueCollection headers)
        {
            using (var ms = new MemoryStream())
            using(var binaryWriter = new BinaryWriter(ms))
            {
                binaryWriter.Write(headers.Count);
                foreach (var key in headers.AllKeys)
                {
                    binaryWriter.Write(key);
                    binaryWriter.Write(headers[key]);
                }
                return ms.ToArray();
            }
        }

        public static NameValueCollection DeserializeHeaders(this byte[] headerBytes)
        {
            var headers = new NameValueCollection();
            if (headerBytes.Length > 24)
            {
                using (var ms = new MemoryStream(headerBytes, 24, headerBytes.Length - 24))
                using (var binaryReader = new BinaryReader(ms))
                {
                    var headerCount = binaryReader.ReadInt32();
                    for (int i = 0; i < headerCount; ++i)
                    {
                        headers.Add(binaryReader.ReadString(), binaryReader.ReadString());
                    }
                }
            }
            return headers;
        }
    }
}