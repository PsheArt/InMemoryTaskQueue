using InMemoryTaskQueue.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class LongRunningTask : ITaskHandler
    {
        public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            for (int i = 0; i < 10; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Задача была отменена");
                    return;
                }
                Console.WriteLine($"Шаг {i}");
                await Task.Delay(500, cancellationToken);
            }
            Console.WriteLine("Задача завершена");
        }
    }
}
