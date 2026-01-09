using InMemoryTaskQueue.Models;
using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, QueuedTask> _tasks = new();
        private readonly ConcurrentDictionary<string, List<QueuedTask>> _partitions = new();
        private readonly SemaphoreSlim _lock = new(1, 1);

        /// <summary>
        /// Кладёт задачу в очередь.
        /// </summary>
        public async Task EnqueueAsync(QueuedTask task, CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                _tasks[task.Id] = task;

                if (task.PartitionKey != null)
                {
                    var partition = _partitions.GetOrAdd(task.PartitionKey, _ => new List<QueuedTask>());
                    partition.Add(task);
                    partition.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Извлекает задачу, срок которой истёк, и готова к выполнению.
        /// Проверяет зависимости и порядок задач в партиции.
        /// </summary>
        public async Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                var now = DateTime.UtcNow;
                var dueTask = _tasks.Values
                    .Where(t => t.DueTime <= now && t.DependsOnTaskId == null || IsDependencyCompleted(t.DependsOnTaskId))
                    .OrderBy(t => t.PartitionKey)
                    .ThenBy(t => t.OrderIndex)
                    .FirstOrDefault();

                if (dueTask != null)
                {
                    _tasks.TryRemove(dueTask.Id, out _);
                    if (dueTask.PartitionKey != null)
                    {
                        var partition = _partitions[dueTask.PartitionKey];
                        partition.Remove(dueTask);
                    }

                    return dueTask;
                }

                return null;
            }
            finally
            {
                _lock.Release();
            }
        }
      
        /// <summary>
        /// Возвращает все задачи для заданной партиции.
        /// </summary>
        public async Task<List<QueuedTask>> GetTasksByPartitionAsync(string partitionKey, CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                return _partitions.TryGetValue(partitionKey, out var tasks) ? tasks : [];
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Возвращает задачу по её идентификатору.
        /// </summary>
        public async Task<QueuedTask?> GetTaskByIdAsync(string taskId, CancellationToken ct = default)
        {
            _tasks.TryGetValue(taskId, out var task);
            return task;
        }

        /// <summary>
        /// Проверяет, завершена ли задача, от которой зависит текущая.
        /// </summary>
        public async Task<bool> IsDependencyCompletedAsync(string dependsOnTaskId, CancellationToken ct = default)
        {
            var task = await GetTaskByIdAsync(dependsOnTaskId, ct);
            return task == null; 
        }

        /// <summary>
        /// Помечает задачу как завершённую.
        /// </summary>
        public async Task MarkAsCompletedAsync(string taskId, CancellationToken ct = default)
        {
            _tasks.TryRemove(taskId, out _);
            foreach (var partition in _partitions.Values)
            {
                partition.RemoveAll(t => t.Id == taskId);
            }
        }

        /// <summary>
        /// Помечает задачу как неудачно выполненную.
        /// </summary>
        public async Task MarkAsFailedAsync(string taskId, Exception exception, CancellationToken ct = default)
        {
            _tasks.TryRemove(taskId, out _);
            foreach (var partition in _partitions.Values)
            {
                partition.RemoveAll(t => t.Id == taskId);
            }
        }

        /// <summary>
        /// Удаляет задачу из очереди.
        /// </summary>
        public async Task DeleteTaskAsync(string taskId, CancellationToken ct = default)
        {
            _tasks.TryRemove(taskId, out _);
            foreach (var partition in _partitions.Values)
            {
                partition.RemoveAll(t => t.Id == taskId);
            }
        }

        /// <summary>
        /// Внутренний метод для проверки завершения зависимости.
        /// </summary>
        private bool IsDependencyCompleted(string? dependsOnTaskId)
        {
            if (dependsOnTaskId == null) return true;
            return !_tasks.ContainsKey(dependsOnTaskId);
        }

        /// <inheritdoc />
        public async Task RequeueAsync(QueuedTask task, CancellationToken ct = default)
        {
            await EnqueueAsync(task, ct);
        }


        /// <summary>
        /// Проверяет наличие задач с меньшим OrderIndex в той же партиции
        /// </summary>
        /// <param name="partitionKey">Ключ партиции</param>
        /// <param name="orderIndex">Текущий индекс порядка</param>
        /// <param name="currentTaskId">Идентификатор текущей задачи (для исключения из проверки)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>True, если есть задачи с меньшим OrderIndex</returns>
        public async Task<bool> HasEarlierTaskAsync(string partitionKey, int orderIndex, string currentTaskId, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (!_partitions.TryGetValue(partitionKey, out var partition))
                    return false;

               
                return partition.Any(t => t.OrderIndex < orderIndex && t.Id != currentTaskId && _tasks.ContainsKey(t.Id)); 
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
