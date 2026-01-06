using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Cancellation
{
    /// <summary>
    /// Регистрирует и управляет токенами отмены задач.
    /// </summary>
    public interface ITaskCancellationRegistry
    {
        /// <summary>
        /// Регистрирует токен отмены для задачи с указанным ID.
        /// </summary>
        /// <param name="taskId">ID задачи.</param>
        /// <returns>Токен отмены.</returns>
        CancellationTokenSource Register(string taskId);

        /// <summary>
        /// Отменяет задачу по ID.
        /// </summary>
        /// <param name="taskId">ID задачи.</param>
        /// <returns>True, если задача была найдена и отменена.</returns>
        bool Cancel(string taskId);

        /// <summary>
        /// Удаляет токен отмены для задачи.
        /// </summary>
        /// <param name="taskId">ID задачи.</param>
        void Unregister(string taskId);
    }
}
