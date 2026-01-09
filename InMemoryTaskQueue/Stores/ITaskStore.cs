using InMemoryTaskQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Stores
{
    /// <summary>
    /// Хранит задачи в очереди.
    /// </summary>
    public interface ITaskStore
    {
        /// <summary>
        /// Добавляет задачу в очередь.
        /// </summary>
        /// <param name="task">Задача для добавления.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Задача завершения.</returns>
        Task EnqueueAsync(QueuedTask task, CancellationToken ct = default);

        /// <summary>
        /// Возвращает задачу обратно в очередь (например, если зависимость не выполнена).
        /// </summary>
        Task RequeueAsync(QueuedTask task, CancellationToken ct = default);
        /// <summary>
        /// Возвращает следующую задачу, срок которой истёк.
        /// </summary>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Задача или null, если нет доступных.</returns>
        Task<QueuedTask?> DequeueDueTaskAsync(CancellationToken ct = default);

        /// <summary>
        /// Помечает задачу как завершённую.
        /// </summary>
        /// <param name="taskId">ID задачи.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Задача завершения.</returns>
        Task MarkAsCompletedAsync(string taskId, CancellationToken ct = default);

        /// <summary>
        /// Помечает задачу как проваленную.
        /// </summary>
        /// <param name="taskId">ID задачи.</param>
        /// <param name="exception">Исключение, вызвавшее сбой.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Задача завершения.</returns>
        Task MarkAsFailedAsync(string taskId, Exception exception, CancellationToken ct = default);

        /// <summary>
        /// Проверяет, завершена ли задача, от которой зависит текущая.
        /// </summary>
        Task<bool> IsDependencyCompletedAsync(string dependsOnTaskId, CancellationToken ct = default);
       
        /// <summary>
        /// Возвращает задачу по её идентификатору.
        /// </summary>
        Task<QueuedTask?> GetTaskByIdAsync(string taskId, CancellationToken ct = default);
        /// <summary>
        /// Удаляет задачу из очереди.
        /// </summary>
        Task DeleteTaskAsync(string taskId, CancellationToken ct = default);
        /// <summary>
        /// Проверяет наличие задач с меньшим OrderIndex в той же партиции
        /// </summary>
        /// <param name="partitionKey">Ключ партиции</param>
        /// <param name="orderIndex">Текущий индекс порядка</param>
        /// <param name="currentTaskId">Идентификатор текущей задачи (для исключения из проверки)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>True, если есть задачи с меньшим OrderIndex</returns>
        Task<bool> HasEarlierTaskAsync(string partitionKey, int orderIndex, string currentTaskId, CancellationToken cancellationToken);
    }
}
