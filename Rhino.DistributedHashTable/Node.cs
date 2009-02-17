namespace Rhino.DistributedHashTable
{
	using System;
	using Castle.MicroKernel.SubSystems.Conversion;

	[Convertible]
	public class Node
	{
		public string Name { get; set; }
		public NodeUri Primary { get; set; }
		public NodeUri Secondary { get; set; }
		public NodeUri Tertiary { get; set; }

		public void ExecuteSync(Action<Uri> action)
		{
			try
			{
				action(Primary.Sync);
			}
			catch (Exception)
			{
				if (Secondary==null)
					throw;
				try
				{
					action(Secondary.Sync);
				}
				catch (Exception)
				{
					if (Tertiary==null)
						throw;
					action(Tertiary.Sync);
				}
			}
		}

		public NodeUri GetOtherReplicationNode(Uri syncUriOfReplicationNode)
		{
			if (Secondary != null && Secondary.Sync == syncUriOfReplicationNode)
				return Tertiary;
			if (Tertiary != null && Tertiary.Sync == syncUriOfReplicationNode)
				return Secondary;
			return null;
		}
	}
}