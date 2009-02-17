namespace Rhino.ServiceBus.DataStructures
{
    public class WireEcryptedString
    {
        public string Value { get; set; }

        public static implicit operator string(WireEcryptedString s)
        {
            return s==null ? null : s.Value;
        }

        public static implicit operator WireEcryptedString(string s)
        {
            return new WireEcryptedString {Value = s};
        }
    }
}