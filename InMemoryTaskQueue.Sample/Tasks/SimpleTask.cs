using InMemoryTaskQueue.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class SimpleTask : ITaskHandler
    {
        public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var id = args["Id"] as int? ?? 0;
            await Task.Delay(1, cancellationToken);
            Console.WriteLine($"[SimpleTask] задача завершена {id}");
        }
    }
}
