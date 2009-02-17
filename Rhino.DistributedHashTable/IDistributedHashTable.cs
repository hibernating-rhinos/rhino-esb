using System;
using System.ServiceModel;

namespace Rhino.DistributedHashTable
{
	using PersistentHashTable;

	[ServiceContract]
	public interface IDistributedHashTable : IDisposable
	{
		Uri Url { get; }

		[OperationContract]
		PutResult[] Put(Node originalDestination, params PutRequest[] valuesToAdd);

		[OperationContract]
		Value[][] Get(params GetRequest[] valuesToGet);

		[OperationContract]
		bool[] Remove(Node originalDestination, params RemoveRequest[] valuesToRemove);
	}
}