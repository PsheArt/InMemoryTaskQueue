using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InMemoryTaskQueue.Strategies;

namespace InMemoryTaskQueue.Models
{
    /// <summary>
    /// Модель задачи, находящейся в очереди.
    /// </summary>
    public class QueuedTask
    {
        /// <summary>
        /// Уникальный идентификатор задачи.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Определение задачи, сериализуемое и восстанавливаемое.
        /// </summary>
        public TaskDefinition TaskDefinition { get; init; } = null!;

        /// <summary>
        /// Время, когда задача должна быть выполнена.
        /// </summary>
        public DateTime DueTime { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Максимальное количество попыток выполнения задачи.
        /// </summary>
        public int MaxRetries { get; init; } = 3;

        /// <summary>
        /// Текущая попытка выполнения задачи.
        /// </summary>
        public int CurrentAttempt { get; set; } = 0;

        /// <summary>
        /// Имя задачи для логгирования или отладки.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Стратегия повтора выполнения задачи.
        /// </summary>
        public IRetryStrategy RetryStrategy { get; init; } = new ExponentialBackoffRetryStrategy();

        /// <summary>
        /// Время создания задачи.
        /// </summary>
        public DateTime? CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Максимальное время выполнения задачи. Если null — без ограничения.
        /// </summary>
        public TimeSpan? ExecutionTimeout { get; init; }

        /// <summary>
        /// Идентификатор источника отмены задачи.
        /// </summary>
        public string? CancellationSourceId { get; init; }

        /// <summary>
        /// Дополнительные пользовательские данные.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; init; }
    }
}
