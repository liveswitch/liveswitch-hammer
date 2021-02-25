using Newtonsoft.Json;
using System;

namespace FM.LiveSwitch.Hammer
{
    class ScanTestResult
    {
        public ScanTestState State { get; protected set; }

        [JsonIgnore]
        public Exception Exception
        {
            get { return _Exception; }
            protected set 
            {
                _Exception = value;

                Reason = ExceptionToReason(value);
            }
        }
        private Exception _Exception;

        public string Reason { get; protected set; }

        private string ExceptionToReason(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            if (exception is AggregateException aggregateException)
            {
                return ExceptionToReason(aggregateException.InnerException);
            }

            var innerException = exception.InnerException;
            if (innerException == null)
            {
                return exception.Message;
            }

            return $"{exception.Message} [{ExceptionToReason(exception.InnerException)}]";
        }
    }
}
