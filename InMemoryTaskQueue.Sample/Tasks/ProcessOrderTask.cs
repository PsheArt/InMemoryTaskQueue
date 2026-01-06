using InMemoryTaskQueue.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class ProcessOrderTask : ITaskHandler
    {
        public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var orderId = args["OrderId"] as string ?? "unknown";
            var amount = args["Amount"] as decimal? ?? 0;

            Console.WriteLine($"[ProcessOrderTask] Процесс заказа {orderId}");
            return Task.CompletedTask;
        }
    }
}
