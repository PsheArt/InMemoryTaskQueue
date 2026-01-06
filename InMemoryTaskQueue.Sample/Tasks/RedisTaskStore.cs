using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Stores;
using System.Text.Json;
using StackExchange.Redis;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class RedisTaskStore : ITaskStore
    {
        private readonly IDatabase _redis;

        public RedisTaskStore(ConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public async Task EnqueueAsync(QueuedTask task, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(task);
            await _redis.ListRightPushAsync("task_queue", json);
        }

        public async Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default)
        {
            var json = await _redis.ListLeftPopAsync("task_queue");
            if (json.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<QueuedTask>(json);
        }

        public async Task MarkAsCompletedAsync(string taskId, CancellationToken ct = default)
        {
            await _redis.SetAddAsync("completed_tasks", taskId);
        }

        public async Task MarkAsFailedAsync(string taskId, Exception exception, CancellationToken ct = default)
        {
            await _redis.SetAddAsync("failed_tasks", taskId);
        }
    }
}
