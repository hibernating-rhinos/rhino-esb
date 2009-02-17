using System;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Tests.Dht
{
    public class InvalidBaristaSaga : 
        ISaga<BaristaState>,
        InitiatedBy<PrepareDrink>
    {
        public Guid Id
        {
            get; set;
        }

        public bool IsCompleted
        {
            get; set;
        }

        public BaristaState State
        {
            get; set;
        }

        public void Consume(PrepareDrink message)
        {
        }
    }
}