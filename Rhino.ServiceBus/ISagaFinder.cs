using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus
{
    /// <summary>
    /// Defines a way to find sagas using messages that don't implement <see cref="ISagaMessage"/>.  
    /// This is useful for when one saga orchestrates messages returned by other consumers or sagas.
    /// </summary>
    /// <typeparam name="SagaT">The type of the saga to find</typeparam>
    /// <typeparam name="MessageT">The message to use </typeparam>
    public interface ISagaFinder<SagaT, MessageT> where SagaT : IAccessibleSaga
    {
        SagaT FindBy(MessageT message);
    }

    /// <summary>
    /// Human readable way of defining a saga finder.
    /// </summary>
    /// <example>
    /// public class MySagaFinder : FinderOf&lt;SagaT&gt;.By&lt;MessageT&gt;
    /// {
    ///		public MySaga FindBy(SpecialMessage message)
    ///		{
    ///			//find sagas using the message here.  Usually you will use an ISagaPersister to fetch sagas.
    ///		}
    /// }
    /// </example>
    /// <typeparam name="SagaT">The type of the saga to find</typeparam>
    public static class FinderOf<SagaT> where SagaT : IAccessibleSaga
    {

        /// <summary>
        /// Human readable way of defining a saga finder.
        /// </summary>
        /// <example>
        /// public class MySagaFinder : FinderOf&lt;SagaT&gt;.By&lt;MessageT&gt;
        /// {
        ///		public MySaga FindBy(SpecialMessage message)
        ///		{
        ///			//find sagas using the message here.  Usually you will use an ISagaPersister to fetch sagas.
        ///		}
        /// }
        /// </example>
        /// <typeparam name="MessageT">The type of the message used to find sagas.</typeparam>
        public interface By<MessageT> : ISagaFinder<SagaT, MessageT>
        {}
    }
}
