using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using log4net;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.LogsService
{
    public class MsmqLogReader : IDisposable
    {
        private readonly string indexDirectory;
        private readonly ILog logger = LogManager.GetLogger(typeof(MsmqLogReader));
        private readonly Uri logQueue;
        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        private int batchCount;
        private MessageQueue queue;

        public MsmqLogReader(Uri logQueue)
        {
            this.logQueue = logQueue;
            indexDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "messages");
        }

        private bool ShouldStop { get; set; }

        #region IDisposable Members

        public void Dispose()
        {
            ShouldStop = true;
            resetEvent.WaitOne();
            if (queue != null)
                queue.Dispose();
        }

        #endregion

        public void Start()
        {
            string queuePath = MsmqUtil.GetQueuePath(logQueue);
            queue = new MessageQueue(queuePath, QueueAccessMode.Receive);
            
            new IndexWriter(indexDirectory, new StandardAnalyzer(), true).Close(true);

            queue.BeginPeek(TimeSpan.FromSeconds(1), new QueueState { Queue = queue, WaitHandle = resetEvent },
                            OnPeekMessage);
        }

        private void OnPeekMessage(IAsyncResult ar)
        {
            Message message;
            bool? peek = TryEndingPeek(ar, out message);
            if (peek == false) // error 
                return;

            var state = (QueueState)ar.AsyncState;
            if (ShouldStop)
            {
                state.WaitHandle.Set();
                return;
            }

            if (peek == null) //nothing was found
            {
                state.Queue.BeginPeek(TimeSpan.FromSeconds(1), state, OnPeekMessage);
                return;
            }
            try
            {
                try
                {
                    ProcessMessages(state);
                }
                catch (MessageQueueException e)
                {
                    if (e.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                        throw;
                }
            }
            catch (Exception e)
            {
                logger.Error("Error when trying to index messages from queue", e);
            }
            state.Queue.BeginPeek(TimeSpan.FromSeconds(1), state, OnPeekMessage);
        }

        /// <summary>
        /// try to read a 100 messages, translate to lucene docs, then write
        /// to index
        /// </summary>
        /// <param name="state"></param>
        private void ProcessMessages(QueueState state)
        {
            int count = 100;
            var list = new List<Message>();

            using (MessageEnumerator enumerator = state.Queue.GetMessageEnumerator2())
            {
                if (enumerator.MoveNext() == false)
                    return;
                while(count > 0)
                {
                    try
                    {
                        list.Add(enumerator.RemoveCurrent());
                    }
                    catch(MessageQueueException e)
                    {
                        if (e.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                            break;
                        throw;
                    }
                }

                var writer = new IndexWriter(indexDirectory, new StandardAnalyzer(), false);
                try
                {
                    foreach (Message message in list)
                    {
                        writer.AddDocument(GetLuceneDocument(message));
                    }

                    if (Interlocked.Increment(ref batchCount) % 15 == 0)
                        writer.Optimize(true);
                }
                finally
                {
                    writer.Close(true);
                }
            }
        }

        private Document GetLuceneDocument(Message message)
        {
            XDocument document = XDocument.Load(XmlReader.Create(message.BodyStream));
            var luceneDoc = new Document();

            foreach (XElement element in document.Root.Elements())
            {
                string name = element.Name.LocalName;
                luceneDoc.Add(new Field(
                                  "MessageAction",
                                  name,
                                  Field.Store.YES,
                                  Field.Index.TOKENIZED));
                logger.DebugFormat("Indexing {0} message", name);
                foreach (XElement value in element.Elements())
                {
                    switch (value.Name.LocalName)
                    {
                        case "MessageId":
                        case "CorrelationId":
                        case "Source":
                        case "Timestamp":
                            luceneDoc.Add(new Field(
                                              value.Name.LocalName,
                                              value.Value,
                                              Field.Store.YES,
                                              Field.Index.TOKENIZED));
                            break;
                        case "MessageType":
                            luceneDoc.Add(new Field(
                                              value.Name.LocalName,
                                              value.Value,
                                              Field.Store.YES,
                                              Field.Index.TOKENIZED));
                            break;
                        case "Message":
                            luceneDoc.Add(new Field(
                                              value.Name.LocalName,
                                              value.ToString(),
                                              //Field.Store.COMPRESS,
                                              Field.Store.YES,
                                              Field.Index.TOKENIZED));
                            break;
                        default:
                            luceneDoc.Add(new Field(
                                              value.Name.LocalName,
                                              value.ToString(),
                                              Field.Store.YES,
                                              Field.Index.UN_TOKENIZED));
                            break;
                    }
                }
            }
            return luceneDoc;
        }

        private bool? TryEndingPeek(IAsyncResult ar, out Message message)
        {
            var state = (QueueState)ar.AsyncState;
            try
            {
                message = state.Queue.EndPeek(ar);
            }
            catch (MessageQueueException e)
            {
                message = null;
                if (e.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                {
                    logger.Error("Could not peek message from queue", e);
                    return false;
                }
                return null; // nothing found
            }
            return true;
        }

        #region Nested type: QueueState

        private class QueueState
        {
            public MessageQueue Queue;
            public ManualResetEvent WaitHandle;
        }

        #endregion
    }
}