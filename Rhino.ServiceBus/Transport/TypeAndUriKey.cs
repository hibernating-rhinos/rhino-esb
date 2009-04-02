using System;

namespace Rhino.ServiceBus.Transport
{
    public class TypeAndUriKey
    {
        public string TypeName;
        public Uri Uri;

        public bool Equals(TypeAndUriKey obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj.TypeName, TypeName) && Equals(obj.Uri, Uri);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(TypeAndUriKey)) return false;
            return Equals((TypeAndUriKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((TypeName != null ? TypeName.GetHashCode() : 0) * 397) ^ (Uri != null ? Uri.GetHashCode() : 0);
            }
        }
    }
}