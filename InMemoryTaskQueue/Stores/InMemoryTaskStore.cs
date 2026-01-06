using InMemoryTaskQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Stores
{
    /// <summary>
    /// In-memory реализация ITaskStore.
    /// </summary>
    public class InMemoryTaskStore : ITaskStore
    {
        private readonly List<QueuedTask> _tasks = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public Task EnqueueAsync(QueuedTask task, CancellationToken ct = default)
        {
            lock (_lock)
            {
                _tasks.Add(task);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var dueTask = _tasks.FirstOrDefault(t => t.DueTime <= now);
                if (dueTask != null)
                {
                    _tasks.Remove(dueTask);
                }
                return Task.FromResult(dueTask);
            }
        }

        /// <inheritdoc />
        public Task MarkAsCompletedAsync(string taskId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task MarkAsFailedAsync(string taskId, Exception exception, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
