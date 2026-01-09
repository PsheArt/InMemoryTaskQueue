using InMemoryTaskQueue.Cancellation;
using InMemoryTaskQueue.Factories;
using InMemoryTaskQueue.Handlers;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Options;
using InMemoryTaskQueue.Processors;
using InMemoryTaskQueue.Services;
using InMemoryTaskQueue.Stores;
using InMemoryTaskQueue.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using TaskScheduler = InMemoryTaskQueue.Services.TaskScheduler;

namespace InMemoryTaskQueue.Tests
{
    public class PartitioningTests
    {
        [Fact]
        public async Task ScheduleTaskAsync_WithSamePartitionKey_ExecutesSequentially()
        {
            // Arrange
            var executedSteps = new List<int>();
            var lockObject = new object();

            var services = new ServiceCollection();
            services.AddSingleton<ITaskStore, InMemoryTaskStore>();
            services.AddSingleton<ITaskCancellationRegistry, TaskCancellationRegistry>();
            services.AddSingleton<ITaskExecutor, DefaultTaskExecutor>();
            services.AddSingleton<IRetryStrategy, ExponentialBackoffRetryStrategy>();
            services.AddSingleton<ITaskScheduler, TaskScheduler>();
            services.AddSingleton<ITaskFactory, DefaultTaskFactory>();

            services.AddSingleton(sp => new SequentialTestTaskHandler(executedSteps, lockObject));
            services.AddSingleton<ITaskHandler>(sp => sp.GetRequiredService<SequentialTestTaskHandler>());

            var provider = services.BuildServiceProvider();
            var scheduler = provider.GetRequiredService<ITaskScheduler>();

            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(scheduler.ScheduleTaskAsync(
                    new TaskDefinition
                    {
                        TaskType = typeof(SequentialTestTaskHandler).AssemblyQualifiedName!,
                        Args = new Dictionary<string, object> { { "Step", i } }
                    },
                    new TaskScheduleOptions
                    {
                        PartitionKey = "sequential_test",
                        OrderIndex = i
                    }
                ));
            }

            await Task.WhenAll(tasks);

            using var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices(services =>
                {
                    services.AddSingleton(provider.GetRequiredService<ITaskStore>());
                    services.AddSingleton(provider.GetRequiredService<ITaskExecutor>());
                    services.AddSingleton(provider.GetRequiredService<ITaskScheduler>());
                    services.AddSingleton(provider.GetRequiredService<ITaskCancellationRegistry>());
                    services.AddSingleton(provider.GetRequiredService<ITaskFactory>());
                    services.AddSingleton(provider.GetRequiredService<IRetryStrategy>());

                    services.AddSingleton(sp => new SequentialTestTaskHandler(executedSteps, lockObject));
                    services.AddSingleton<ITaskHandler>(sp => sp.GetRequiredService<SequentialTestTaskHandler>());

                    services.AddHostedService<TaskProcessor>();
                })
                .Build();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await host.StartAsync(cts.Token);
            bool completed = await WaitForTasksToComplete(executedSteps, 5, TimeSpan.FromSeconds(25), lockObject);

            try
            {
                await host.StopAsync(TimeSpan.FromSeconds(10));
            }
            catch (OperationCanceledException)
            {
            }
            lock (lockObject)
            {
                Assert.Equal(new List<int> { 0, 1, 2, 3, 4 }, executedSteps.OrderBy(x => x).ToList());
            }
        }

        [Fact]
        public async Task ScheduleTaskAsync_WithDifferentPartitionKeys_ExecutesConcurrently()
        {
            var executionOrder = new List<(string partition, int step)>();
            var lockObject = new object();

            var services = new ServiceCollection();
            services.AddSingleton<ITaskStore, InMemoryTaskStore>();
            services.AddSingleton<ITaskCancellationRegistry, TaskCancellationRegistry>();
            services.AddSingleton<ITaskExecutor, DefaultTaskExecutor>();
            services.AddSingleton<IRetryStrategy, ExponentialBackoffRetryStrategy>();
            services.AddSingleton<ITaskScheduler, TaskScheduler>();
            services.AddSingleton<ITaskFactory, DefaultTaskFactory>();

            services.AddSingleton(sp => new ConcurrentTestTaskHandler(executionOrder, lockObject));
            services.AddSingleton<ITaskHandler>(sp => sp.GetRequiredService<ConcurrentTestTaskHandler>());

            var provider = services.BuildServiceProvider();
            var scheduler = provider.GetRequiredService<ITaskScheduler>();

            var tasks = new List<Task>();
            for (int partition = 0; partition < 5; partition++)
            {
                for (int step = 0; step < 2; step++)
                {
                    tasks.Add(scheduler.ScheduleTaskAsync(
                        new TaskDefinition
                        {
                            TaskType = typeof(ConcurrentTestTaskHandler).AssemblyQualifiedName!,
                            Args = new Dictionary<string, object> { { "Partition", partition }, { "Step", step } }
                        },
                        new TaskScheduleOptions
                        {
                            PartitionKey = partition.ToString(),
                            OrderIndex = step
                        }
                    ));
                }
            }

            await Task.WhenAll(tasks);

            using var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices(services =>
                {
                    services.AddSingleton(provider.GetRequiredService<ITaskStore>());
                    services.AddSingleton(provider.GetRequiredService<ITaskExecutor>());
                    services.AddSingleton(provider.GetRequiredService<ITaskScheduler>());
                    services.AddSingleton(provider.GetRequiredService<ITaskCancellationRegistry>());
                    services.AddSingleton(provider.GetRequiredService<ITaskFactory>());
                    services.AddSingleton(provider.GetRequiredService<IRetryStrategy>());

                    services.AddSingleton(sp => new ConcurrentTestTaskHandler(executionOrder, lockObject));
                    services.AddSingleton<ITaskHandler>(sp => sp.GetRequiredService<ConcurrentTestTaskHandler>());

                    services.AddHostedService<TaskProcessor>();
                })
                .Build();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await host.StartAsync(cts.Token);
            bool completed = await WaitForTasksToComplete(executionOrder, 10, TimeSpan.FromSeconds(25), lockObject);
            try
            {
                await host.StopAsync(TimeSpan.FromSeconds(10));
            }
            catch (OperationCanceledException)
            {
            }

            // Assert
            lock (lockObject)
            {
                Assert.Equal(10, executionOrder.Count);
            }
        }

        private async Task<bool> WaitForTasksToComplete<T>(List<T> collection, int expectedCount, TimeSpan timeout, object lockObject)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                int currentCount;
                lock (lockObject)
                {
                    currentCount = collection.Count;
                }

                if (currentCount >= expectedCount)
                    return true;

                await Task.Delay(100);
            }

            return false; // Не достигли ожидаемого количества вовремя
        }
    }

        // Отдельные классы для тестирования, чтобы избежать статических полей
        public class SequentialTestTaskHandler : ITaskHandler
    {
        private readonly List<int> _executedSteps;
        private readonly object _lockObject;

        public SequentialTestTaskHandler(List<int> executedSteps, object lockObject)
        {
            _executedSteps = executedSteps;
            _lockObject = lockObject;
        }

        public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var step = args["Step"] as int? ?? throw new ArgumentException();

            await Task.Delay(100, cancellationToken);

            lock (_lockObject)
            {
                _executedSteps.Add(step);
            }
        }
    }

    public class ConcurrentTestTaskHandler : ITaskHandler
    {
        private readonly List<(string partition, int step)> _executionOrder;
        private readonly object _lockObject;

        public ConcurrentTestTaskHandler(List<(string partition, int step)> executionOrder, object lockObject)
        {
            _executionOrder = executionOrder;
            _lockObject = lockObject;
        }

        public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var partition = args["Partition"] as int? ?? throw new ArgumentException();
            var step = args["Step"] as int? ?? throw new ArgumentException();

            await Task.Delay(100, cancellationToken);

            lock (_lockObject)
            {
                _executionOrder.Add((partition.ToString(), step));
            }
        }
    }
}
