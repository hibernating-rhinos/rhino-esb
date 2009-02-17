namespace Rhino.DistributedHashTable
{
	using System;
	using System.ServiceModel;

	[ServiceContract]
	public interface IDistributedHashTableMetaDataProvider
	{
		Uri Url { get; }

		[OperationContract]
		Node[] GetNetworkNodes();
		
		[OperationContract]
		Node GetNodeByUri(Uri nodeUri);
	}
}