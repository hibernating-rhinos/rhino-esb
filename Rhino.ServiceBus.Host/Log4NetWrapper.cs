using System;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Host
{
    public class Log4NetWrapper : ILog
    {
        private readonly log4net.ILog _underlying;

        public Log4NetWrapper(Type typ)
        {
            _underlying = log4net.LogManager.GetLogger(typ);
        }

        public void Info(string message)
        {
            _underlying.Info(message);
        }

        public void Warn(string message)
        {
            _underlying.Warn(message);
        }

        public void Warn(string message, Exception exception)
        {
            _underlying.Warn(message, exception);
        }

        public void Debug(string message)
        {
            _underlying.Debug(message);
        }

        public void Debug(string message, Exception exception)
        {
            _underlying.Info(message, exception);
        }

        public void Error(string message)
        {
            _underlying.Error(message);
        }

        public void Error(Exception exception)
        {
            _underlying.Error(exception);
        }

        public void Error(string message, Exception exception)
        {
            _underlying.Info(message, exception);
        }

        public void Fatal(string message)
        {
            _underlying.Fatal(message);
        }

        public void Fatal(string message, Exception exception)
        {
            _underlying.Fatal(message, exception);
        }
    }
}