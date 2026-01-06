using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InMemoryTaskQueue.Strategies;

namespace InMemoryTaskQueue.Options
{
    /// <summary>
    /// Опции планирования задачи.
    /// </summary>
    public class TaskScheduleOptions
    {
        /// <summary>
        /// Задержка перед выполнением задачи.
        /// </summary>
        public TimeSpan Delay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Максимальное количество попыток выполнения задачи.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Стратегия повтора выполнения задачи.
        /// </summary>
        public IRetryStrategy? RetryStrategy { get; set; }

        /// <summary>
        /// Имя задачи для логгирования или отладки.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Максимальное время выполнения задачи. Если null — без ограничения.
        /// </summary>
        public TimeSpan? ExecutionTimeout { get; set; }

        /// <summary>
        /// Дополнительные пользовательские данные.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
