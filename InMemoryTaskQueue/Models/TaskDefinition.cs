using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Models
{
    /// <summary>
    /// Сериализуемое определение задачи. 
    /// </summary>
    public class TaskDefinition
    {
        /// <summary>
        /// Тип задачи, по которому можно восстановить её реализацию.
        /// </summary>
        public string TaskType { get; init; } = null!;

        /// <summary>
        /// Аргументы, необходимые для выполнения задачи.
        /// </summary>
        public Dictionary<string, object> Args { get; init; } = new();

    }
}
