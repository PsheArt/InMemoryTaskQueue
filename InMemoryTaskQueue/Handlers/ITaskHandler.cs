using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Handlers
{
    /// <summary>
    /// Интерфейс обработчика задачи, который может быть зарегистрирован в DI.
    /// </summary>
    public interface ITaskHandler
    {
        /// <summary>
        /// Выполняет задачу с указанными аргументами.
        /// </summary>
        /// <param name="args">Аргументы задачи.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача выполнения.</returns>
        Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken);
    }
}
