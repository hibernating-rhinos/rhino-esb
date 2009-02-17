namespace Rhino.DistributedHashTable
{
	using Castle.MicroKernel.SubSystems.Conversion;

	[Convertible]
	public class Network
	{
		public NetworkNode[] Nodes { get; set; }
	}
}