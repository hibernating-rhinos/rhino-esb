namespace Rhino.DistributedHashTable
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.ServiceModel;

	[ServiceBehavior(
		ConcurrencyMode = ConcurrencyMode.Multiple,
		InstanceContextMode = InstanceContextMode.Single,
		IncludeExceptionDetailInFaults = true
	)]
	public class DistributedHashTableMetaDataProvider : IDistributedHashTableMetaDataProvider
	{
		private readonly Node[] nodes;
		public DistributedHashTableMetaDataProvider(Network network, Uri url)
		{
			nodes = new Node[network.Nodes.Length];

			for (int i = 0; i < network.Nodes.Length; i++)
			{
				var node = network.Nodes[i];
				if (string.IsNullOrEmpty(node.Name))
					throw new ArgumentException("Node name cannot be empty. Node #" + i);
				nodes[i] = new Node
				{
                    Name = node.Name,
					Primary = GetNode(network.Nodes, node.Name),
					Secondary = GetNode(network.Nodes, node.Secondary),
					Tertiary = GetNode(network.Nodes, node.Tertiary),
				};
			}

			Url = url;
		}

		private NodeUri GetNode(IEnumerable<NetworkNode> networkNodes, string name)
		{
			if (name == null)
				return null;
			var node = networkNodes.Where(x => x.Name == name).FirstOrDefault();
			if(node==null)
				throw new ArgumentException("Could not find node named: " + name);
			return new NodeUri
			{
				Async = node.Async,
                Sync = node.Sync
			};
		}

		public Uri Url
		{
			get;
			private set;
		}

		public Node[] GetNetworkNodes()
		{
			return nodes;
		}

		public Node GetNodeByUri(Uri nodeUri)
		{
			return nodes.Where(x => x.Primary.Sync == nodeUri).FirstOrDefault();
		}
	}
}