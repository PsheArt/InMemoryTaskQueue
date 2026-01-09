using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Options;
using InMemoryTaskQueue.Strategies;
using InMemoryTaskQueue.Cancellation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InMemoryTaskQueue.Stores;

namespace InMemoryTaskQueue.Services
{
    /// <summary>
    /// Реализация планировщика задач.
    /// </summary>
    /// <inheritdoc />
    public class TaskScheduler(
        ITaskStore taskStore,
        ITaskCancellationRegistry cancellationRegistry,
        ILogger<TaskScheduler>? logger = null) : ITaskScheduler
    {
        private readonly ITaskStore _taskStore = taskStore;
        private readonly ITaskCancellationRegistry _cancellationRegistry = cancellationRegistry;
        private readonly ILogger<TaskScheduler>? _logger = logger;

        /// <inheritdoc />
        public async Task<string> ScheduleTaskAsync(
            TaskDefinition taskDefinition,
            TaskScheduleOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new TaskScheduleOptions();

            var queuedTask = new QueuedTask
            {
                TaskDefinition = taskDefinition,
                DueTime = DateTime.UtcNow + options.Delay,
                MaxRetries = options.MaxRetries,
                Name = options.Name,
                RetryStrategy = options.RetryStrategy ?? new ExponentialBackoffRetryStrategy(),
                ExecutionTimeout = options.ExecutionTimeout,
                Metadata = options.Metadata,
                CancellationSourceId = Guid.NewGuid().ToString(),                        
                PartitionKey = options.PartitionKey,
                DependsOnTaskId = options.DependsOnTaskId, 
                OrderIndex = options.OrderIndex 
            };

            await _taskStore.EnqueueAsync(queuedTask, cancellationToken);

            _logger?.LogInformation("Задача {TaskId} запланирован", queuedTask.Id);

            return queuedTask.Id;
        }

        /// <inheritdoc />
        public Task<bool> CancelTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var result = _cancellationRegistry.Cancel(taskId);
            if (result)
            {
                _logger?.LogInformation("Задача {TaskId} была отменена.", taskId);
            }
            else
            {
                _logger?.LogWarning("Задача {TaskId} не найденаы", taskId);
            }
            return Task.FromResult(result);
        }
    }
}
