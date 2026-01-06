using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Strategies
{
    /// <summary>
    /// Определяет, как и когда повторять задачу при ошибке.
    /// </summary>
    public interface IRetryStrategy
    {
        /// <summary>
        /// Возвращает задержку до следующей попытки или null, если повторять не нужно.
        /// </summary>
        /// <param name="currentAttempt">Номер текущей попытки.</param>
        /// <param name="lastException">Последнее исключение, вызвавшее сбой.</param>
        /// <returns>Время задержки перед следующей попыткой или null.</returns>
        TimeSpan? GetNextDelay(int currentAttempt, Exception? lastException);
    }
}
