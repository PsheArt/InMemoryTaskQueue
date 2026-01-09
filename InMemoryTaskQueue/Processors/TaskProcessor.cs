using InMemoryTaskQueue.Options;
using InMemoryTaskQueue.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InMemoryTaskQueue.Stores;
using System.Collections.Concurrent;
using InMemoryTaskQueue.Models;

namespace InMemoryTaskQueue.Processors
{
    /// <summary>
    /// Фоновая служба, опрашивающая очередь задач и выполняющая их.
    /// </summary>
    public class TaskProcessor : BackgroundService, IDisposable
    {
        private readonly ITaskStore _taskStore;
        private readonly ITaskExecutor _taskExecutor;
        private readonly ILogger<TaskProcessor> _logger;
        private readonly InMemoryTaskQueueOptions _options;
        private readonly SemaphoreSlim _semaphore;
        private readonly KeyedSemaphore _keyedSemaphore = new();

        // Список активных задач 
        private readonly ConcurrentBag<Task> _activeTasks = new();
        // Внутренний CancellationTokenSource
        private readonly CancellationTokenSource _internalCts = new();

        // Поле для отслеживания состояния освобождения
        private bool _disposed = false;

        /// <inheritdoc />
        public TaskProcessor(ITaskStore taskStore, ITaskExecutor taskExecutor, ILogger<TaskProcessor> logger, IOptions<InMemoryTaskQueueOptions> options)
        {
            _taskStore = taskStore;
            _taskExecutor = taskExecutor;
            _logger = logger;
            _options = options.Value;
            _semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
        }

        /// <summary>
        /// Основной метод выполнения фоновой задачи - опрашивает очередь и обрабатывает задачи
        /// </summary>
        /// <param name="stoppingToken">Токен отмены для остановки сервиса</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && !_internalCts.Token.IsCancellationRequested)
            {
                try
                {
                    var task = await _taskStore.DequeueDueTaskAsync(stoppingToken);

                    if (task != null)
                    {
                        if (task.DependsOnTaskId != null)
                        {
                            var isDependencyCompleted = await _taskStore.IsDependencyCompletedAsync(task.DependsOnTaskId, stoppingToken);
                            if (!isDependencyCompleted)
                            {
                                await _taskStore.RequeueAsync(task, stoppingToken);
                                await Task.Delay(10, stoppingToken);
                                continue;
                            }
                        }

                        if (task.PartitionKey != null)
                        {
                            var earlierTaskExists = await _taskStore.HasEarlierTaskAsync(task.PartitionKey, task.OrderIndex, task.Id, stoppingToken);
                            if (earlierTaskExists)
                            {
                                await _taskStore.RequeueAsync(task, stoppingToken);
                                await Task.Delay(10, stoppingToken);
                                continue;
                            }
                        }

                        var taskToRun = ProcessTaskAsync(task, stoppingToken);
                        _activeTasks.Add(taskToRun);

                        RemoveCompletedTasks();
                    }
                    else
                    {
                        await Task.Delay(100, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в TaskProcessor");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        /// <summary>
        /// Асинхронно обрабатывает отдельную задачу с учетом ограничений по параллелизму и партиционированию
        /// </summary>
        /// <param name="task">Задача для обработки</param>
        /// <param name="cancellationToken">Токен отмены</param>
        private async Task ProcessTaskAsync(QueuedTask task, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (task.PartitionKey != null)
                {
                    var semaphore = _keyedSemaphore.GetSemaphore(task.PartitionKey);
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await _taskExecutor.ExecuteAsync(task, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
                else
                {
                    await _taskExecutor.ExecuteAsync(task, cancellationToken);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        /// <summary>
        /// Удаляет завершенные задачи из списка активных
        /// </summary>
        private void RemoveCompletedTasks()
        {
            var completedTasks = _activeTasks.Where(t => t.IsCompleted).ToList();
            foreach (var completedTask in completedTasks)
            {
                _activeTasks.TryTake(out _);
            }
        }
        /// <summary>
        /// Останавливает сервис с корректным завершением активных задач
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TaskProcessor is stopping...");

            try
            {
                _internalCts.Cancel();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                while (!_activeTasks.IsEmpty && !timeoutTask.IsCompletedSuccessfully)
                {
                    RemoveCompletedTasks();

                    if (!_activeTasks.IsEmpty)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                    else
                    {
                        break;
                    }
                }
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            _logger.LogInformation("TaskProcessor остановился успешно. Активная задача удалена: {_activeTasks.Count}", _activeTasks.Count);
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        /// <param name="disposing">true, если вызывается из Dispose(), false - из финализатора</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _semaphore?.Dispose();
                    _internalCts?.Dispose();

                    // Освобождаем семафоры в keyedSemaphore
                    _keyedSemaphore?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Публичный метод Dispose для интерфейса IDisposable
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Класс для управления семафорами по ключу (для партиционирования).
    /// </summary>
    public class KeyedSemaphore : IDisposable
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

        /// <summary>
        /// Возвращает или создает семафор для заданного ключа
        /// </summary>
        /// <param name="key">Ключ для партиционирования</param>
        /// <returns>Семафор, соответствующий ключу</returns>
        public SemaphoreSlim GetSemaphore(string key)
        {
            return _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Освобождает все семафоры
        /// </summary>
        public void Dispose()
        {
            foreach (var semaphore in _semaphores.Values)
            {
                semaphore?.Dispose();
            }
            _semaphores.Clear();
        }
    }

}
