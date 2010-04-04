using System;
using System.Messaging;
using System.Transactions;
using log4net;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Msmq
{
	public class MessageHandlingCompletion
	{
		private readonly Message message;
		private readonly TransactionScope tx;
		private readonly OpenedQueue messageQueue;
		private readonly Action<CurrentMessageInformation, Exception> messageCompleted;
		private readonly Action<CurrentMessageInformation> beforeTransactionCommit;
		private readonly ILog logger;
		private readonly Action<CurrentMessageInformation, Exception> messageProcessingFailure;
		private readonly CurrentMessageInformation currentMessageInformation;

		private Exception exception;

		public MessageHandlingCompletion(Message message, TransactionScope tx, OpenedQueue messageQueue, Exception exception, Action<CurrentMessageInformation, Exception> messageCompleted, Action<CurrentMessageInformation> beforeTransactionCommit, ILog logger, Action<CurrentMessageInformation, Exception> messageProcessingFailure, CurrentMessageInformation currentMessageInformation)
		{
			this.message = message;
			this.tx = tx;
			this.messageQueue = messageQueue;
			this.exception = exception;
			this.messageCompleted = messageCompleted;
			this.beforeTransactionCommit = beforeTransactionCommit;
			this.logger = logger;
			this.messageProcessingFailure = messageProcessingFailure;
			this.currentMessageInformation = currentMessageInformation;
		}


		public void HandleMessageCompletion()
		{
			var txDisposed = false;
			if (exception == null)
			{
				try
				{
					if (tx != null)
					{
						if (beforeTransactionCommit != null)
							beforeTransactionCommit(currentMessageInformation);
						tx.Complete();
						tx.Dispose();
						txDisposed = true;
					}
					try
					{
						if (messageCompleted != null)
							messageCompleted(currentMessageInformation, exception);
					}
					catch (Exception e)
					{
						logger.Error("An error occured when raising the MessageCompleted event, the error will NOT affect the message processing", e);
					}
					return;
				}
				catch (Exception e)
				{
					logger.Warn("Failed to complete transaction, moving to error mode", e);
					exception = e;
				}
			}
			try
			{
				if (txDisposed == false && tx != null)
				{
					logger.Warn("Disposing transaction in error mode");
					tx.Dispose();
				}
			}
			catch (Exception e)
			{
				logger.Warn("Failed to dispose of transaction in error mode.", e);
			}
			if (message == null)
				return;


			try
			{
				if (messageCompleted != null)
					messageCompleted(currentMessageInformation, exception);
			}
			catch (Exception e)
			{
				logger.Error("An error occured when raising the MessageCompleted event, the error will NOT affect the message processing", e);
			}

			try
			{
				if (messageProcessingFailure != null)
					messageProcessingFailure(currentMessageInformation, exception);
			}
			catch (Exception moduleException)
			{
				logger.Error("Module failed to process message failure: " + exception.Message,
				             moduleException);
			}

			if (messageQueue.IsTransactional == false)// put the item back in the queue
			{
				messageQueue.Send(message);
			}
		}

	}
}