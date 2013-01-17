using System;
using Rhino.ServiceBus.Util;

namespace Rhino.ServiceBus
{
    public class Endpoint
    {
        private Uri uri;

    	public bool? Transactional;

        public Uri Uri
        {
            get { return uri; }
            set 
            {
                if (uri == null) throw new ArgumentNullException();
                uri = value.NormalizeLocalhost(); 
            }
        }

        public override string ToString()
        {
            return string.Format("Uri: {0}", Uri);
        }
    }
}