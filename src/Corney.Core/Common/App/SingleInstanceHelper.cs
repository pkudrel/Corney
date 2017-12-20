using System;
using Corney.Core.Common.Infrastructure;
using NLog;

namespace Corney.Core.Common.App
{
    public class SingleInstanceHelper
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public static void ProcessFailedSingleInstance(Attempt<SingleGlobalInstance> attempt)
        {
            string message;
            switch (attempt.Exception)
            {
                case TimeoutException timeoutException:
                    message = "Cannot create new app instance. Probably is already running";
                    break;
                case NullReferenceException argumentNullException:
                    message = argumentNullException.Message;
                    break;
                default:
                    message = attempt.Exception.Message;
                    break;
            }
            _log.Error(message);
            _log.Error(attempt.Exception);

            if (!Environment.UserInteractive) return;
 
        }
    }
}