using InMemoryTaskQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Factories
{
    /// <summary>
    /// Фабрика для восстановления задач из сериализуемого определения.
    /// </summary>
    public interface ITaskFactory
    {
        /// <summary>
        /// Восстанавливает асинхронную задачу по её определению.
        /// </summary>
        /// <param name="taskDefinition">Сериализуемое определение задачи.</param>
        /// <returns>Функция выполнения задачи.</returns>
        Func<CancellationToken, Task> CreateTask(TaskDefinition taskDefinition);
    }
}
