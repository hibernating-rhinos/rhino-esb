namespace Rhino.DistributedHashTable
{
	using System;

	public static class Replication
	{
		private static readonly Node node = new Node
		{
			Name = "From Replication",
			Primary = new NodeUri
			{
				Sync = new Uri("null://end/of/the/world?turn=left")
			}
		};

		public static Node Node
		{
			get { return node; }
		}
	}
}