using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Options;

namespace InMemoryTaskQueue.Services
{
    /// <summary>
    /// Предоставляет методы для планирования асинхронных задач.
    /// </summary>
    public interface ITaskScheduler
    {
        /// <summary>
        /// Планирует выполнение задачи через ITaskHandler.
        /// </summary>
        /// <param name="taskDefinition">Определение задачи.</param>
        /// <param name="options">Опции выполнения.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>ID задачи для отслеживания.</returns>
        Task<string> ScheduleTaskAsync(TaskDefinition taskDefinition,TaskScheduleOptions? options = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// Отменяет задачу по ID.
        /// </summary>
        /// <param name="taskId">ID задачи.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>True, если задача была найдена и отменена.</returns>
        Task<bool> CancelTaskAsync(string taskId, CancellationToken cancellationToken = default);
    }
}
