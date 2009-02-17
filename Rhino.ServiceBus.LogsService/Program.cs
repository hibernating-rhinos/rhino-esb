using System;
using log4net.Config;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace Rhino.ServiceBus.LogsService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BasicConfigurator.Configure();
            //using (var reader = new MsmqLogReader(new Uri("msmq://localhost/test_queue2")))
            //{
            //    reader.Start();

            //    Console.ReadLine();
            //}

            var searcher = new IndexSearcher("messages");
            var parser = new QueryParser("", new StandardAnalyzer());
            //var query = parser.Parse("MessageId:\"b5005080-800c-43c3-a20b-16db773d7663\" AND MessageId:2307015");
            var query = parser.Parse("Timestamp:[\"2008-12-16T08:14:53.9749900\" TO \"2008-12-16T08:14:53.6343650\"]");
            
            var hits = searcher.Search(query);
            for (int i = 0; i < hits.Length(); i++)
            {
                var doc = hits.Doc(i);
                Console.WriteLine();
                foreach (Fieldable field in doc.GetFields())
                {
                    Console.WriteLine("{0}: {1}", field.Name(), field.StringValue());
                }
                Console.WriteLine();
            }
        }
    }
}