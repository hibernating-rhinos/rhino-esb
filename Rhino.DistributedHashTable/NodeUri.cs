namespace Rhino.DistributedHashTable
{
	using System;
	using Castle.MicroKernel.SubSystems.Conversion;

	[Convertible]
	public class NodeUri
	{
		public Uri Sync { get; set; }
		public Uri Async { get; set; }
	}
}