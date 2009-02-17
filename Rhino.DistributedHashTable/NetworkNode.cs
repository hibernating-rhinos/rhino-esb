namespace Rhino.DistributedHashTable
{
	using System;
	using Castle.MicroKernel.SubSystems.Conversion;

	[Convertible]
	public class NetworkNode
	{
		public Uri Sync { get; set; }
		public Uri Async { get; set; }
		public string Name { get; set; }

		public string Secondary { get; set; }
		public string Tertiary { get; set; }
	}
}