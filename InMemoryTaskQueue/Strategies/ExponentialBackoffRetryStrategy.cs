using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Strategies
{
    /// <summary>
    /// Стратегия повтора с экспоненциальным увеличением задержки.
    /// </summary>
    public class ExponentialBackoffRetryStrategy : IRetryStrategy
    {
        /// <inheritdoc />
        public TimeSpan? GetNextDelay(int currentAttempt, Exception? lastException)
        {
            if (currentAttempt >= 3) return null;

            var delay = TimeSpan.FromSeconds(Math.Pow(2, currentAttempt));
            return delay;
        }
    }
}
