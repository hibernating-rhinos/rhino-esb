namespace Rhino.DistributedHashTable
{
	using System;
	using System.ServiceModel;

	public static class Binding
	{
		public static NetTcpBinding DhtDefault
		{
			get
			{
			    var binding = new NetTcpBinding
			    {
			        OpenTimeout = TimeSpan.FromMilliseconds(500),
			        CloseTimeout = TimeSpan.FromMilliseconds(250),
			    };
			    binding.ReaderQuotas.MaxArrayLength = 1024*1024*3;//3MB or so
			    return binding;
			}
		}
	}
}