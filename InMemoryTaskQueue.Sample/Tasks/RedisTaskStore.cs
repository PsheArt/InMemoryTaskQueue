using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Stores;
using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class RedisTaskStore : ITaskStore
    {
        private readonly IDatabase _redis;
        private readonly ILogger<RedisTaskStore>? _logger;

        public RedisTaskStore(ConnectionMultiplexer redis, ILogger<RedisTaskStore>? logger = null)
        {
            _redis = redis.GetDatabase();
            _logger = logger;
        }

        public async Task EnqueueAsync(QueuedTask task, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(task);
            await _redis.HashSetAsync($"task:{task.Id}", new HashEntry[]
            {
            new HashEntry("data", json),
            new HashEntry("partition", task.PartitionKey ?? ""),
            new HashEntry("order_index", task.OrderIndex.ToString())
            });

            await _redis.ListRightPushAsync($"queue:{task.PartitionKey ?? "default"}", task.Id);
            await _redis.SortedSetAddAsync("due_time_queue", task.Id, task.DueTime.Ticks);

            _logger?.LogInformation("Задача {TaskId} помещена в очередь", task.Id);
        }

        public async Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var dueTasks = await _redis.SortedSetRangeByScoreAsync("due_time_queue", 0, now.Ticks);

            foreach (var taskId in dueTasks)
            {
                var task = await GetTaskByIdAsync(taskId, ct);

                if (task != null)
                {
                    await _redis.SortedSetRemoveAsync("due_time_queue", taskId);
                    await _redis.HashDeleteAsync($"task:{taskId}", new RedisValue[] { "data", "partition", "order_index" });

                    return task;
                }
            }

            return null;
        }

        public async Task RequeueAsync(QueuedTask task, CancellationToken ct = default)
        {
            await EnqueueAsync(task, ct);
        }

        public async Task<List<QueuedTask>> GetTasksByPartitionAsync(string partitionKey, CancellationToken ct = default)
        {
            var taskIds = await _redis.ListRangeAsync($"queue:{partitionKey}");
            var tasks = new List<QueuedTask>();

            foreach (var taskId in taskIds)
            {
                var task = await GetTaskByIdAsync(taskId, ct);
                if (task != null) tasks.Add(task);
            }

            tasks.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
            return tasks;
        }

        public async Task<QueuedTask?> GetTaskByIdAsync(string taskId, CancellationToken ct = default)
        {
            var hash = await _redis.HashGetAllAsync($"task:{taskId}");
            if (hash.Length == 0) return null;

            var jsonData = hash.FirstOrDefault(h => h.Name == "data").Value;
            if (jsonData.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<QueuedTask>(jsonData);
        }

        public async Task MarkAsCompletedAsync(string taskId, CancellationToken ct = default)
        {
            await _redis.SortedSetRemoveAsync("due_time_queue", taskId);
            await _redis.HashDeleteAsync($"task:{taskId}", new RedisValue[] { "data", "partition", "order_index" });
            _logger?.LogInformation("Task {TaskId} marked as completed.", taskId);
        }

        public async Task MarkAsFailedAsync(string taskId, Exception exception, CancellationToken ct = default)
        {
            await _redis.SortedSetRemoveAsync("due_time_queue", taskId);
            await _redis.HashDeleteAsync($"task:{taskId}", new RedisValue[] { "data", "partition", "order_index" });
            _logger?.LogError(exception, "Task {TaskId} marked as failed.", taskId);
        }

        public async Task DeleteTaskAsync(string taskId, CancellationToken ct = default)
        {
            await _redis.SortedSetRemoveAsync("due_time_queue", taskId);
            await _redis.HashDeleteAsync($"task:{taskId}", new RedisValue[] { "data", "partition", "order_index" });
            _logger?.LogInformation("Task {TaskId} deleted.", taskId);
        }

        public Task<bool> IsDependencyCompletedAsync(string dependsOnTaskId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
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
            var taskIds = await _redis.ListRangeAsync($"queue:{partitionKey}");

            foreach (var taskId in taskIds)
            {
                if (taskId == currentTaskId) continue;

                var orderIndexStr = await _redis.HashGetAsync($"task:{taskId}", "order_index");
                if (int.TryParse(orderIndexStr, out var taskOrderIndex) && taskOrderIndex < orderIndex)
                {
                    var status = await _redis.HashGetAsync($"task:{taskId}", "status");
                    if (status.IsNullOrEmpty || status == "pending") 
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}