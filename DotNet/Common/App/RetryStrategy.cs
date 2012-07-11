using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.App
{
    public struct RetryStrategy
    {
        public const int DefaultMaxTries = 3;
        public int MaxTries;
        public Func<Exception, bool> IsRetryable;
        public Action<Exception> RecoveryAction;

        /// <summary>
        /// Executes an action and retries if an exception occurs for a number of times, applying the recovery action before a retry.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="retryStrategy">A RetryStrategy object that specifies how to handle exceptions and retry the action.</param>
        /// <returns>The return value of the action to execute.</returns>
        public object Execute(Func<object> action)
        {
            int numTries = 0;
            object result = null;

            while (true)
            {
                try
                {
                    result = action();
                    break;
                }
                catch (Exception ex)
                {
                    numTries++;
                    if (numTries < this.MaxTries && null != this.IsRetryable && this.IsRetryable(ex))
                    {
                        if (null != this.RecoveryAction)
                            this.RecoveryAction(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return result;
        }
    }
}
