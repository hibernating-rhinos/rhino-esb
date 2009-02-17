using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas.Persisters;

namespace Starbucks.Customer
{
    public class CustomerBootStrapper : AbstractBootStrapper
    {
        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            container.Register(
                Component.For(typeof(ISagaPersister<>))
                    .ImplementedBy(typeof(InMemorySagaPersister<>))
                );
        }

        protected override bool IsTypeAcceptableForThisBootStrapper(System.Type t)
        {
            return t.Namespace == "Starbucks.Customer";
        }
    }
}
