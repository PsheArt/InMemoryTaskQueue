using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Cancellation
{
    /// <summary>
    /// Реализация ITaskCancellationRegistry с использованием словаря.
    /// </summary>
    public class TaskCancellationRegistry : ITaskCancellationRegistry
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();

        /// <inheritdoc />
        public CancellationTokenSource Register(string taskId)
        {
            var cts = new CancellationTokenSource();
            _tokens[taskId] = cts;
            return cts;
        }

        /// <inheritdoc />
        public bool Cancel(string taskId)
        {
            if (_tokens.TryGetValue(taskId, out var cts))
            {
                cts.Cancel();
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void Unregister(string taskId)
        {
            if (_tokens.TryRemove(taskId, out var cts))
            {
                cts.Dispose();
            }
        }
    }
}
