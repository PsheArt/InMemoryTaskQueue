using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Options
{
    /// <summary>
    /// Настройки планировщика задач.
    /// </summary>
    public class InMemoryTaskQueueOptions
    {
        /// <summary>
        /// Максимальное количество задач, выполняемых одновременно.
        /// </summary>
        public int MaxConcurrency { get; set; } = 10;

        /// <summary>
        /// Интервал опроса хранилища задач.
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Включить логгирование.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Максимальный размер очереди задач.
        /// </summary>
        public int MaxQueueSize { get; set; } = 10000;
    }
}
