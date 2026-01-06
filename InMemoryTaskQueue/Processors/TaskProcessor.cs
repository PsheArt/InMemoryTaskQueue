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

namespace InMemoryTaskQueue.Processors
{
    /// <summary>
    /// Фоновая служба, опрашивающая очередь задач и выполняющая их.
    /// </summary>
    public class TaskProcessor : BackgroundService
    {
        private readonly ITaskStore _taskStore;
        private readonly ITaskExecutor _taskExecutor;
        private readonly ILogger<TaskProcessor> _logger;
        private readonly InMemoryTaskQueueOptions _options;
        private readonly SemaphoreSlim _semaphore;

        /// <inheritdoc />
        public TaskProcessor(ITaskStore taskStore, ITaskExecutor taskExecutor, ILogger<TaskProcessor> logger, IOptions<InMemoryTaskQueueOptions> options)
        {
            _taskStore = taskStore;
            _taskExecutor = taskExecutor;
            _logger = logger;
            _options = options.Value;
            _semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var task = await _taskStore.DequeueDueTaskAsync(stoppingToken);
                if (task != null)
                {
                    _ = Task.Run(async () =>
                    {
                        await _semaphore.WaitAsync(stoppingToken);
                        try
                        {
                            await _taskExecutor.ExecuteAsync(task, stoppingToken);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    });
                }

                await Task.Delay(_options.PollingInterval, stoppingToken);
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TaskProcessor is stopping...");
            await _semaphore.WaitAsync(cancellationToken);
            _semaphore.Release(); 
            await base.StopAsync(cancellationToken);
        }
    }
}
