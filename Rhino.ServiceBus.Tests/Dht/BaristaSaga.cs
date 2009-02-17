using System;
using System.Threading;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Tests.Dht
{
    public class BaristaSaga :
        ISaga<BaristaState>,
        InitiatedBy<PrepareDrink>,
        Orchestrates<PaymentComplete>,
        Orchestrates<MergeSagaState>
    {
        public static ManualResetEvent FinishedConsumingMessage;
        public static ManualResetEvent WaitToCreateConflicts;

        public static Guid SagaId { get; set; }
        public static string DrinkName;

        private readonly IServiceBus bus;

        public BaristaSaga(IServiceBus bus)
        {
            this.bus = bus;
            State = new BaristaState();
        }

        #region InitiatedBy<PrepareDrink> Members

        public void Consume(PrepareDrink message)
        {
            State.Drink = message.DrinkName;

            State.DrinkIsReady = true;
            SubmitOrderIfDone();
        }

        #endregion

        public BaristaState State { get; set; }

        public Guid Id { get; set; }

        public bool IsCompleted { get; set; }

        public void Consume(PaymentComplete message)
        {
            State.GotPayment = true;
            SubmitOrderIfDone();
        }

        private void SubmitOrderIfDone()
        {
            if (State.GotPayment && State.DrinkIsReady)
            {
                bus.Publish(new DrinkReady
                {
                    CorrelationId = Id,
                    Drink = State.Drink
                });
                IsCompleted = true;
            }
            SagaId = Id;
            FinishedConsumingMessage.Set();
            WaitToCreateConflicts.WaitOne(TimeSpan.FromSeconds(30));
        }

        public void Consume(MergeSagaState message)
        {
            DrinkName = State.Drink;
            FinishedConsumingMessage.Set();
        }
    }
}