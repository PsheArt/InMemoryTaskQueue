using InMemoryTaskQueue.Factories;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Cancellation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;

namespace InMemoryTaskQueue.Services
{
    /// <summary>
    /// Стандартная реализация ITaskExecutor, использующая фабрику задач.
    /// </summary>
    public class DefaultTaskExecutor : ITaskExecutor
    {
        private readonly ITaskFactory _taskFactory;
        private readonly ITaskCancellationRegistry _cancellationRegistry;
        private readonly ILogger<DefaultTaskExecutor>? _logger;
        private readonly Meter? _meter;
        private readonly Counter<long>? _completedCounter;
        private readonly Counter<long>? _failedCounter;

        /// <inheritdoc />
        public DefaultTaskExecutor(
            ITaskFactory taskFactory,
            ITaskCancellationRegistry cancellationRegistry,
            ILogger<DefaultTaskExecutor>? logger = null,
            IMeterFactory? meterFactory = null)
        {
            _taskFactory = taskFactory;
            _cancellationRegistry = cancellationRegistry;
            _logger = logger;

            if (meterFactory != null)
            {
                _meter = meterFactory.Create("InMemoryTaskQueue"); 
                if (_meter != null) 
                {
                    _completedCounter = _meter.CreateCounter<long>("tasks.completed");
                    _failedCounter = _meter.CreateCounter<long>("tasks.failed");
                }
            }
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(QueuedTask task, CancellationToken cancellationToken)
        {
            Func<CancellationToken, Task>? func = null;

            try
            {
                func = _taskFactory.CreateTask(task.TaskDefinition);
                System.Diagnostics.Debug.WriteLine(func);

                if (func == null)
                {
                    _logger?.LogError("Task factory returned null for task {TaskId}.", task.Id);
                    throw new InvalidOperationException($"Task factory returned null for task {task.Id}.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create task function for task {TaskId}.", task.Id);
                throw;
            }
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            System.Diagnostics.Debug.WriteLine(cts);
            if (task.CancellationSourceId != null)
            {
                var taskCts = _cancellationRegistry.Register(task.CancellationSourceId);
                cts.Token.Register(() => taskCts.Cancel()); 
            }

            try
            {
                if (task.ExecutionTimeout.HasValue)
                {
                    await func(cts.Token).WaitAsync(task.ExecutionTimeout.Value, cts.Token);
                }
                else
                {
                    await func(cts.Token);
                }

                _completedCounter?.Add(1, new KeyValuePair<string, object?>("task_id", task.Id));
                _logger?.LogInformation("Task {TaskId} completed successfully.", task.Id);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Задача {TaskId} была отменена.", task.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Задача {TaskId} упала с ошибкой.", task.Id);
                throw;
            }
            finally
            {
                if (task.CancellationSourceId != null)
                {
                    _cancellationRegistry.Unregister(task.CancellationSourceId);
                }
            }
        }
    }
}
