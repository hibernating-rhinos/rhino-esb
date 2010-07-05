using System.Collections.Specialized;

namespace Rhino.ServiceBus
{
    public interface ICustomizeMessageHeaders
    {
        void Customize(NameValueCollection headers);
    }
}