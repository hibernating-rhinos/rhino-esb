using Rhino.ServiceBus.DataStructures;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.DataStructures
{
    public class LRUSetTest
    {
        [Fact]
        public void Can_check_that_an_item_is_in_the_set()
        {
            var set = new LeastRecentlyUsedSet<string>();
            set.Write((add, remove) =>
            {
                add("blah");
                add("foo");
                add("bar");
            });

            Assert.True(set.Contains("foo"));
        }

        [Fact]
        public void Can_remove_items()
        {
            var set = new LeastRecentlyUsedSet<string>();
            set.Write((add,remove) =>
            {
                add("blah");
                add("foo");
                add("bar");
                remove("foo");
            });

            Assert.False(set.Contains("foo"));
        }

        [Fact]
        public void Can_iterate_items()
        {
            var set = new LeastRecentlyUsedSet<string>();

            set.Write((add, remove) =>
            {
                add("blah");
                add("foo");
                add("bar");
            });

            var array = set.OrderBy(x => x).ToArray();//iterate & order

            Assert.Equal("bar", array[0]);
            Assert.Equal("blah", array[1]);
            Assert.Equal("foo", array[2]);
        }

        [Fact]
        public void Cannot_have_more_than_limited_amount_in_set()
        {
            var set = new LeastRecentlyUsedSet<string>(10);

            set.Write((add, remove) =>
            {
                for (int i = 0; i < 20; i++)
                {
                    add(i.ToString());
                }
            }); 

            var length = set.Count();

            Assert.Equal(10, length);
        }

        [Fact]
        public void When_selecting_what_to_remove_from_set_will_select_oldest_items()
        {
            var set = new LeastRecentlyUsedSet<string>(10);

            set.Write((add, remove) =>
            {
                for (int i = 0; i < 20; i++)
                {
                    add(i.ToString());
                }
            }); 
            for (int i = 10; i < 20; i++)
            {
                Assert.True(set.Contains(i.ToString()));
            }
        }
    }
}