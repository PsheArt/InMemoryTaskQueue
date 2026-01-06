using System;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Stores;
using System.Text.Json;
using Confluent.Kafka;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class KafkaTaskStore : ITaskStore
    {
        private readonly IProducer<string, string> _producer;
        private readonly IConsumer<string, string> _consumer;

        public async Task EnqueueAsync(QueuedTask task, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(task);
            await _producer.ProduceAsync("task_queue", new Message<string, string> { Key = task.Id, Value = json });
        }

        public async Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default)
        {
            var consumeResult = _consumer.Consume(ct);
            return JsonSerializer.Deserialize<QueuedTask>(consumeResult.Message.Value);
        }

        public Task MarkAsCompletedAsync(string taskId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task MarkAsFailedAsync(string taskId, Exception exception, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
