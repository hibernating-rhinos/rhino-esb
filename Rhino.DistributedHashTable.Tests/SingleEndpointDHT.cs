namespace Rhino.DistributedHashTable.Tests
{
	using Castle.Windsor.Configuration.Interpreters;
	using PersistentHashTable;
	using Xunit;
	using System.Linq;

	public class SingleEndpointDHT : DhtTestBase
	{
		public SingleEndpointDHT() : base(new XmlInterpreter())
		{
		}

		[Fact]
		public void Can_add_and_recieve_item()
		{
			client.Put(
		        new PutRequest
		        {
		            Key = "abc", 
		            Bytes = new byte[] {123},
                    ParentVersions = new ValueVersion[0]
		        }
		    );

			var values = client.Get(
		        new GetRequest
		        {
		            Key = "abc",
		        });
			Assert.Equal(1, values[0].Length);
			Assert.Equal(new byte[] { 123 }, values[0][0].Data);
			
		}

		[Fact]
		public void Can_add_and_recieve_lots_of_items()
		{
			for (int i = 0; i < 500; i++)
			{
				client.Put(
		            new PutRequest()
		            {
		                Key = "abc" + i,
		                Bytes = new byte[] {123},
                        ParentVersions = new ValueVersion[0]
		            });

				var values = client.Get(
		            new GetRequest
		            {
		                Key = "abc" + i,
		            });
				Assert.Equal(1, values[0].Length);
				Assert.Equal(new byte[] { 123 }, values[0][0].Data);
			}
		}


		[Fact]
		public void Can_remove_value()
		{
			var versions = client.Put(
		        new PutRequest()
		        {
		            Key = "abc", 
		            Bytes = new byte[] {123},
                    ParentVersions = new ValueVersion[0]
		        });

			var removed = client.Remove(
		        new RemoveRequest()
		        {
		            Key = "abc",
		            ParentVersions = versions.Select(x=>x.Version).ToArray()
		        });
			Assert.True(removed[0]);

			var values = client.Get(
		        new GetRequest()
		        {
		            Key = "abc",
		        });
			Assert.Equal(0, values[0].Length);
		}
	}
}