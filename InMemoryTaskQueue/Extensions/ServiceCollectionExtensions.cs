using InMemoryTaskQueue.Factories;
using InMemoryTaskQueue.Options;
using InMemoryTaskQueue.Stores;
using InMemoryTaskQueue.Strategies;
using InMemoryTaskQueue.Cancellation;
using InMemoryTaskQueue.Services;
using InMemoryTaskQueue.Processors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Extensions
{
    /// <summary>
    /// Методы расширения для регистрации компонентов планировщика задач в DI-контейнере.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет компоненты планировщика задач в сервисы.
        /// </summary>
        /// <param name="services">Коллекция сервисов.</param>
        /// <param name="configureOptions">Конфигурация опций.</param>
        /// <returns>Коллекция сервисов.</returns>
        public static IServiceCollection AddInMemoryTaskQueue(this IServiceCollection services,Action<InMemoryTaskQueueOptions>? configureOptions = null)
        {
            services.Configure(configureOptions ?? (_ => { }));

            services.AddSingleton<ITaskStore, InMemoryTaskStore>();
            services.AddSingleton<ITaskCancellationRegistry, TaskCancellationRegistry>();
            services.AddSingleton<ITaskExecutor, DefaultTaskExecutor>();
            services.AddSingleton<IRetryStrategy, ExponentialBackoffRetryStrategy>();
            services.AddSingleton<ITaskScheduler, InMemoryTaskQueue.Services.TaskScheduler>();
            services.AddSingleton<ITaskFactory, DefaultTaskFactory>();
            services.AddHostedService<TaskProcessor>();

            return services;
        }
    }
}
