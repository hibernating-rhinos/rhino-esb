namespace Rhino.DistributedHashTable.Tests
{
	using PersistentHashTable;
	using Xunit;
	using System.Linq;

	public class LocalDistributedHashTableTests 
	{
		public LocalDistributedHashTableTests()
		{
			DhtTestBase.Delete("test.esent");
		}

		[Fact]
		public void Can_save_and_get_item()
		{
			using(var lht = new LocalDistributedHashTable("test.esent"))
			{
				lht.Put(new PutRequest
				{
					Key = "abc",
					Bytes = new byte[] {1, 3, 6},
					ParentVersions = new ValueVersion[0]
				});

				var values = lht.Get(new GetRequest{Key = "abc"});
				Assert.Equal(new byte[]{1,3,6}, values[0][0].Data);
			}
		}

		[Fact]
		public void Can_remove_item()
		{
			using (var lht = new LocalDistributedHashTable("test.esent"))
			{
				var put = lht.Put(new PutRequest
				{
					Key = "abc",
					Bytes = new byte[] { 1, 3, 6 },
					ParentVersions = new ValueVersion[0]
				});

				lht.Remove(new RemoveRequest
				{
					Key = "abc",
					ParentVersions = put.Select(x => x.Version).ToArray()
				});

				var values = lht.Get(new GetRequest { Key = "abc" });
				Assert.Equal(0, values[0].Length);
			}
		}
	}
}