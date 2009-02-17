namespace Rhino.DistributedHashTable.Messages
{
	using PersistentHashTable;

	public class RemoveRequests
	{
		public RemoveRequest[] Requests { get; set; }
	}
}