using InMemoryTaskQueue.Handlers;
using InMemoryTaskQueue.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Factories
{
    /// <summary>
    /// Стандартная реализация фабрики задач, использующая DI-контейнер для поиска обработчиков.
    /// </summary>
    public class DefaultTaskFactory : ITaskFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        public DefaultTaskFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public Func<CancellationToken, Task> CreateTask(TaskDefinition taskDefinition)
        {
            var handlerType = Type.GetType(taskDefinition.TaskType);

            if (handlerType == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    handlerType = assembly.GetType(taskDefinition.TaskType);
                    if (handlerType != null) break;
                }
            }

            if (handlerType == null || !typeof(ITaskHandler).IsAssignableFrom(handlerType))
            {
                throw new InvalidOperationException($"Task handler type '{taskDefinition.TaskType}' not found or does not implement ITaskHandler.");
            }

            var handler = (ITaskHandler)_serviceProvider.GetRequiredService(handlerType);

            return ct => handler.ExecuteAsync(taskDefinition.Args, ct);
        }
    }
}
