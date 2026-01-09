using System;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Stores;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class KafkaTaskStore : ITaskStore
    {
        private readonly IProducer<string, string> _producer;
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<KafkaTaskStore>? _logger;

        public KafkaTaskStore(IProducer<string, string> producer, IConsumer<string, string> consumer, ILogger<KafkaTaskStore>? logger = null)
        {
            _producer = producer;
            _consumer = consumer;
            _logger = logger;
        }

        public async Task EnqueueAsync(QueuedTask task, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(task);
            await _producer.ProduceAsync($"task_queue_{task.PartitionKey ?? "default"}", new Message<string, string> { Key = task.Id, Value = json });

            _logger?.LogInformation("Task {TaskId} enqueued with partition {PartitionKey}.", task.Id, task.PartitionKey);
        }

        public async Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default)
        {
            var consumeResult = _consumer.Consume(ct);
            var task = JsonSerializer.Deserialize<QueuedTask>(consumeResult.Message.Value);

            if (task != null)
            {
                return task;
            }

            return null;
        }

        public async Task RequeueAsync(QueuedTask task, CancellationToken ct = default)
        {
            await EnqueueAsync(task, ct);
        }

        public async Task<List<QueuedTask>> GetTasksByPartitionAsync(string partitionKey, CancellationToken ct = default)
        {
            throw new NotImplementedException("Kafka не поддерживает получение всех задач по партиции.");
        }

        public async Task<QueuedTask?> GetTaskByIdAsync(string taskId, CancellationToken ct = default)
        {
            throw new NotImplementedException("Kafka не поддерживает получение задачи по ID.");
        }

        public async Task MarkAsCompletedAsync(string taskId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task MarkAsFailedAsync(string taskId, Exception exception, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteTaskAsync(string taskId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
