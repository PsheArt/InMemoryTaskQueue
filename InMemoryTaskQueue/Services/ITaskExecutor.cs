using InMemoryTaskQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Services
{
    /// <summary>
    /// Отвечает за выполнение задачи и обработку ошибок.
    /// </summary>
    public interface ITaskExecutor
    {
        /// <summary>
        /// Выполняет задачу.
        /// </summary>
        /// <param name="task">Задача для выполнения.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача выполнения.</returns>
        Task ExecuteAsync(QueuedTask task, CancellationToken cancellationToken);
    }
}
