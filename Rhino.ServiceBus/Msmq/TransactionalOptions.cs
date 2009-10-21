namespace Rhino.ServiceBus.Msmq
{
	/// <summary>
	/// This is required because Windsor doesn't allow to pass nulls as valid arguments
	/// to ctors
	/// </summary>
	public enum TransactionalOptions
	{
		Transactional,
		NonTransactional,
		FigureItOut
	}
}