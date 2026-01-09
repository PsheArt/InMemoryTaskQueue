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
            var handlerType = ResolveType(taskDefinition.TaskType);

            if (handlerType == null)
            {
                throw new InvalidOperationException($"Тип задачи '{taskDefinition.TaskType}' не найден.");
            }

            if (!typeof(ITaskHandler).IsAssignableFrom(handlerType))
            {
                throw new InvalidOperationException($"Тип '{taskDefinition.TaskType}' не реализует интерфейс ITaskHandler.");
            }

            var handler = _serviceProvider.GetService(handlerType) as ITaskHandler;
            if (handler == null)
            {
                try
                {
                    handler = (ITaskHandler)ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Не удалось создать экземпляр обработчика задачи '{taskDefinition.TaskType}'.", ex);
                }
            }

            return ct => handler.ExecuteAsync(taskDefinition.Args, ct);
        }
        private Type? ResolveType(string typeName)
        {
            var handlerType = Type.GetType(typeName);

            if (handlerType != null)
            {
                return handlerType;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    handlerType = assembly.GetType(typeName);
                    if (handlerType != null)
                        return handlerType;
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }
    }
}
