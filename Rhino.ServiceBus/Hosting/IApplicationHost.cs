using System;

namespace Rhino.ServiceBus.Hosting
{
    public interface IApplicationHost : IDisposable
    {
        void Start(string assembly);
        void InitialDeployment(string assembly, string user);
        void SetBootStrapperTypeName(string type);
    }
}