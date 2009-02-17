using System.Diagnostics;

namespace Rhino.ServiceBus.Messages
{
    public class AddSubscription : AdministrativeMessage
    {
        public string Type { get; set; }
        private string endpoint;
        public string Endpoint
        {
            get { return endpoint; }
            set
            {
                if(value.StartsWith("Uri:"))
                    Debugger.Break();
                
                endpoint = value;
            }
        }
    }
}