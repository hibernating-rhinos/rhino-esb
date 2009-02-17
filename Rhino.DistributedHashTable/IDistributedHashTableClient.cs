namespace Rhino.DistributedHashTable
{
	using PersistentHashTable;

	public interface IDistributedHashTableClient
	{
		PutResult[] Put(params PutRequest[] valuesToAdd);
		Value[][] Get(params GetRequest[] valuesToGet);
		bool[] Remove(params RemoveRequest[] valuesToRemove);
	}
}