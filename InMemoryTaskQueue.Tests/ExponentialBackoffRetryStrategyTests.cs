using InMemoryTaskQueue.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InMemoryTaskQueue.Tests
{
    public class ExponentialBackoffRetryStrategyTests
    {
        [Fact]
        public void GetNextDelay_ReturnsCorrectDelay()
        {
            var strategy = new ExponentialBackoffRetryStrategy();

            var delay1 = strategy.GetNextDelay(1, null);
            var delay2 = strategy.GetNextDelay(2, null);

            Assert.Equal(TimeSpan.FromSeconds(2), delay1);
            Assert.Equal(TimeSpan.FromSeconds(4), delay2);
        }

        [Fact]
        public void GetNextDelay_ReturnsNullAfterMaxRetries()
        {
            var strategy = new ExponentialBackoffRetryStrategy();

            var delay = strategy.GetNextDelay(4, null);

            Assert.Null(delay);
        }
    }
}
