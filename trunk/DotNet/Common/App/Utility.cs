using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.App
{
    public static class Utility
    {
        public static int MaxTries = 3;

        /// <summary>
        /// Executes an action and retries if an exception occurs, applying the recovery action before a retry.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="shouldThrow">A function that evaluates whether an exception should be thrown immediately.</param>
        /// <param name="recoveryAction">The recovery action to apply before a retry.</param>
        /// <returns>The return value of the action to execute.</returns>
        public static object ExecuteWithRetry(Func<object> action, Func<Exception, bool> shouldThrow, Action recoveryAction)
        {
            return ExecuteWithRetry(action, shouldThrow, recoveryAction, MaxTries);
        }

        /// <summary>
        /// Executes an action and retries if an exception occurs for a number of times, applying the recovery action before a retry.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="shouldThrow">A function that evaluates whether an exception should be thrown immediately.</param>
        /// <param name="recoveryAction">The recovery action to apply before a retry.</param>
        /// <param name="maxTries">The number of times to try executing the action.</param>
        /// <returns>The return value of the action to execute.</returns>
        public static object ExecuteWithRetry(Func<object> action, Func<Exception, bool> shouldThrow, Action recoveryAction, int maxTries)
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
                    if ((numTries >= maxTries) || (shouldThrow != null && shouldThrow(ex)))
                        throw;
                    else
                        recoveryAction();
                }
            }

            return result;
        }
    }
}
