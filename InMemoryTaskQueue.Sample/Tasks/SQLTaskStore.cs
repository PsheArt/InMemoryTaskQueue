using System;
using System.Data;
using System.Text.Json;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Stores;
using Dapper;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class SQLTaskStore : ITaskStore
    {
        private readonly IDbConnection _connection;

        public SQLTaskStore(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task EnqueueAsync(QueuedTask task, CancellationToken ct = default)
        {
            var sql = @"
            INSERT INTO scheduled_tasks (id, definition, due_time, created_at)
            VALUES (@Id, @Definition, @DueTime, @CreatedAt)";

            var definitionJson = JsonSerializer.Serialize(task.TaskDefinition);
            await _connection.ExecuteAsync(sql, new
            {
                Id = task.Id,
                Definition = definitionJson,
                DueTime = task.DueTime,
                CreatedAt = task.CreatedAt
            });
        }

        public async Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default)
        {
            var sql = @"
            SELECT TOP 1 * FROM scheduled_tasks
            WHERE due_time <= @Now AND status = 'pending'
            ORDER BY due_time";

            var row = await _connection.QueryFirstOrDefaultAsync(sql, new { Now = DateTime.UtcNow });

            if (row == null) return null;

            var definition = JsonSerializer.Deserialize<TaskDefinition>(row.Definition);
            return new QueuedTask
            {
                Id = row.Id,
                TaskDefinition = definition,
                DueTime = row.DueTime
            };
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
