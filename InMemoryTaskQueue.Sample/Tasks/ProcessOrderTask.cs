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
    public class OrderValidationTask : ITaskHandler
    {
        public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var orderId = args["OrderId"] as string ?? "unknown";
            var step = args["Step"] as string ?? "unknown";

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Начало {step} для заказа {orderId}");

            // Симуляция валидации
            await Task.Delay(2000, cancellationToken);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Завершено {step} для заказа {orderId}");
        }
    }

    public class PaymentProcessingTask : ITaskHandler
    {
        public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var orderId = args["OrderId"] as string ?? "unknown";
            var step = args["Step"] as string ?? "unknown";
            var amount = args["Amount"] as decimal? ?? 0;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Начало {step} для заказа {orderId} на сумму {amount:C}");

            // Симуляция обработки платежа
            await Task.Delay(3000, cancellationToken);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Завершено {step} для заказа {orderId} на сумму {amount:C}");
        }
    }

    public class InventoryUpdateTask : ITaskHandler
    {
        public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var orderId = args["OrderId"] as string ?? "unknown";
            var step = args["Step"] as string ?? "unknown";
            var productId = args["ProductId"] as string ?? "unknown";
            var quantity = args["Quantity"] as int? ?? 0;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Начало {step} для заказа {orderId}, товар {productId}, количество {quantity}");

            // Симуляция обновления инвентаря
            await Task.Delay(1500, cancellationToken);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Завершено {step} для заказа {orderId}, товар {productId}, количество {quantity}");
        }
    }
}
