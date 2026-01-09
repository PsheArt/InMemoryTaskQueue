using System;
using System.Data;
using System.Text.Json;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Stores;
using Dapper;
using Microsoft.Extensions.Logging;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class SQLTaskStore : ITaskStore
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<SQLTaskStore>? _logger;

        public SQLTaskStore(IDbConnection connection, ILogger<SQLTaskStore>? logger = null)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task EnqueueAsync(QueuedTask task, CancellationToken ct = default)
        {
            var sql = @"
            INSERT INTO scheduled_tasks (id, definition, due_time, created_at, max_retries, partition_key, order_index)
            VALUES (@Id, @Definition, @DueTime, @CreatedAt, @MaxRetries, @PartitionKey, @OrderIndex)";

            var definitionJson = JsonSerializer.Serialize(task.TaskDefinition);
            await _connection.ExecuteAsync(sql, new
            {
                Id = task.Id,
                Definition = definitionJson,
                DueTime = task.DueTime,
                CreatedAt = task.CreatedAt,
                MaxRetries = task.MaxRetries,
                PartitionKey = task.PartitionKey,
                OrderIndex = task.OrderIndex
            });

            _logger?.LogInformation("Task {TaskId} enqueued with partition {PartitionKey}.", task.Id, task.PartitionKey);
        }

        public async Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default)
        {
            using var transaction = _connection.BeginTransaction();

            try
            {
                var sql = @"
                UPDATE scheduled_tasks
                SET status = 'processing'
                WHERE id = (
                    SELECT id FROM scheduled_tasks
                    WHERE due_time <= @Now AND status = 'pending'
                    ORDER BY partition_key, order_index
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING id, definition, due_time, partition_key, order_index";

                var row = await _connection.QueryFirstOrDefaultAsync(sql, new { Now = DateTime.UtcNow }, transaction);

                transaction.Commit();

                if (row == null) return null;

                var definition = JsonSerializer.Deserialize<TaskDefinition>(row.definition);
                return new QueuedTask
                {
                    Id = row.id,
                    TaskDefinition = definition,
                    DueTime = row.due_time,
                    PartitionKey = row.partition_key,
                    OrderIndex = row.order_index
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task RequeueAsync(QueuedTask task, CancellationToken ct = default)
        {
            // В ..SQL задача не возвращается в очередь
            await EnqueueAsync(task, ct); //поэтому помещаем в очередь
        }

        public async Task<List<QueuedTask>> GetTasksByPartitionAsync(string partitionKey, CancellationToken ct = default)
        {
            var sql = @"
            SELECT * FROM scheduled_tasks
            WHERE partition_key = @PartitionKey
            ORDER BY order_index";

            var rows = await _connection.QueryAsync(sql, new { PartitionKey = partitionKey });

            return rows.Select(row =>
            {
                var definition = JsonSerializer.Deserialize<TaskDefinition>(row.definition);
                return new QueuedTask
                {
                    Id = row.id,
                    TaskDefinition = definition,
                    DueTime = row.due_time,
                    PartitionKey = row.partition_key,
                    OrderIndex = row.order_index
                };
            }).ToList();
        }

        public async Task<QueuedTask?> GetTaskByIdAsync(string taskId, CancellationToken ct = default)
        {
            var sql = "SELECT * FROM scheduled_tasks WHERE id = @TaskId";

            var row = await _connection.QueryFirstOrDefaultAsync(sql, new { TaskId = taskId });

            if (row == null) return null;

            var definition = JsonSerializer.Deserialize<TaskDefinition>(row.definition);
            return new QueuedTask
            {
                Id = row.id,
                TaskDefinition = definition,
                DueTime = row.due_time,
                PartitionKey = row.partition_key,
                OrderIndex = row.order_index
            };
        }

        public async Task MarkAsCompletedAsync(string taskId, CancellationToken ct = default)
        {
            var sql = "UPDATE scheduled_tasks SET status = 'completed' WHERE id = @TaskId";

            await _connection.ExecuteAsync(sql, new { TaskId = taskId });

            _logger?.LogInformation("Задача {TaskId} помечсена как завершенная .", taskId);
        }

        public async Task MarkAsFailedAsync(string taskId, Exception exception, CancellationToken ct = default)
        {
            var sql = "UPDATE scheduled_tasks SET status = 'failed', error_message = @ErrorMessage WHERE id = @TaskId";

            await _connection.ExecuteAsync(sql, new { TaskId = taskId, ErrorMessage = exception.Message });

            _logger?.LogError(exception, "Задача {TaskId} marked как упавшей.", taskId);
        }

        public async Task DeleteTaskAsync(string taskId, CancellationToken ct = default)
        {
            var sql = "DELETE FROM scheduled_tasks WHERE id = @TaskId";

            await _connection.ExecuteAsync(sql, new { TaskId = taskId });

            _logger?.LogInformation("Задача {TaskId} удалена.", taskId);
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
            var sql = @"
        SELECT COUNT(*) 
        FROM scheduled_tasks 
        WHERE partition_key = @PartitionKey 
        AND order_index < @OrderIndex 
        AND id != @CurrentTaskId 
        AND status = 'pending'";

            var count = await _connection.QuerySingleAsync<int>(sql, new
            {
                PartitionKey = partitionKey,
                OrderIndex = orderIndex,
                CurrentTaskId = currentTaskId
            });

            return count > 0;
        }
    }
}