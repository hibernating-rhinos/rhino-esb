namespace Rhino.ServiceBus.DataStructures
{
    public class WireEncryptedString
    {
        public string Value { get; set; }

        public static implicit operator string(WireEncryptedString s)
        {
            return s==null ? null : s.Value;
        }

        public static implicit operator WireEncryptedString(string s)
        {
            return new WireEncryptedString {Value = s};
        }
    }
}