using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas.Persisters;

namespace Starbucks.Cashier
{
    public class CashierBootStrapper : CastleBootStrapper
    {
        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            Container.Register(
                            Component.For(typeof(ISagaPersister<>))
                                .ImplementedBy(typeof(InMemorySagaPersister<>))
                            );
        }

        protected override bool IsTypeAcceptableForThisBootStrapper(System.Type t)
        {
            return t.Namespace == "Starbucks.Cashier";
        }
    }
}
