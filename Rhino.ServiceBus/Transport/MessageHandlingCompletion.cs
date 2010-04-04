using System;
using System.Transactions;
using log4net;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Transport
{
	public class MessageHandlingCompletion
	{
		private readonly TransactionScope tx;
		private readonly Action sendMessageBackToQueue;
		private readonly Action<CurrentMessageInformation, Exception> messageCompleted;
		private readonly Action<CurrentMessageInformation> beforeTransactionCommit;
		private readonly ILog logger;
		private readonly Action<CurrentMessageInformation, Exception> messageProcessingFailure;
		private readonly CurrentMessageInformation currentMessageInformation;

		private Exception exception;

		public MessageHandlingCompletion(TransactionScope tx, Action sendMessageBackToQueue, Exception exception, Action<CurrentMessageInformation, Exception> messageCompleted, Action<CurrentMessageInformation> beforeTransactionCommit, ILog logger, Action<CurrentMessageInformation, Exception> messageProcessingFailure, CurrentMessageInformation currentMessageInformation)
		{
			this.tx = tx;
			this.sendMessageBackToQueue = sendMessageBackToQueue;
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

			try
			{
				if (SuccessfulCompletion(out txDisposed))
					return;
			}
			finally
			{
				DisposeTransactionIfNotAlreadyDisposed(txDisposed);				
			}

			//error

			NotifyMessageCompleted();

			NotifyAboutMessageProcessingFailure();

			SendMessageBackToQueue();
		}

		private void SendMessageBackToQueue()
		{
			if (sendMessageBackToQueue != null)
				sendMessageBackToQueue();
		}

		private void NotifyMessageCompleted()
		{
			try
			{
				if (messageCompleted != null)
					messageCompleted(currentMessageInformation, exception);
			}
			catch (Exception e)
			{
				logger.Error("An error occured when raising the MessageCompleted event, the error will NOT affect the message processing", e);
			}
		}

		private void NotifyAboutMessageProcessingFailure()
		{
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
		}

		private void DisposeTransactionIfNotAlreadyDisposed(bool txDisposed)
		{
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
		}

		private bool SuccessfulCompletion(out bool txDisposed)
		{
			txDisposed = false;
			if (exception != null)
				return false;
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
				NotifyMessageCompleted();
				return true;
			}
			catch (Exception e)
			{
				logger.Warn("Failed to complete transaction, moving to error mode", e);
				exception = e;
			}
			return false;
		}
	}
}