using InMemoryTaskQueue.Handlers;
using InMemoryTaskQueue.Options;
using InMemoryTaskQueue.Sample.Tasks;
using InMemoryTaskQueue.Extensions;
using InMemoryTaskQueue.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InMemoryTaskQueue.Models;

var builder = new HostBuilder().ConfigureServices((context, services) =>
{
    // Добавить планировщик задач
    services.AddInMemoryTaskQueue(options =>
    {
        options.MaxConcurrency = 5;
        options.PollingInterval = TimeSpan.FromMilliseconds(200);
    });

    // Регистрация обработчиков задач
    services.AddSingleton<ITaskHandler, SendEmailTask>();
    services.AddSingleton<SendEmailTask>();
    services.AddSingleton<ITaskHandler, ProcessOrderTask>();
    services.AddSingleton<ProcessOrderTask>();
    services.AddSingleton<ITaskHandler, LongRunningTask>();
    services.AddSingleton<LongRunningTask>();
    services.AddSingleton<ITaskHandler, SimpleTask>();
    services.AddSingleton<SimpleTask>();

    // Добавляем обработчики для примеров с Partition
    services.AddSingleton<ITaskHandler, OrderValidationTask>();
    services.AddSingleton<OrderValidationTask>();
    services.AddSingleton<ITaskHandler, PaymentProcessingTask>();
    services.AddSingleton<PaymentProcessingTask>();
    services.AddSingleton<ITaskHandler, InventoryUpdateTask>();
    services.AddSingleton<InventoryUpdateTask>();
    services.AddSingleton<ITaskHandler, UserNotificationTask>();
    services.AddSingleton<UserNotificationTask>();
});

var host = builder.Build();

var scheduler = host.Services.GetRequiredService<ITaskScheduler>();

Console.WriteLine("=== Samples ===");

#region Простая задача через ITaskHandler
Console.WriteLine("Планирование задачи SendEmailTask...");
var taskId1 = await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(SendEmailTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object>
        {
            { "Email", "user@example.com" },
            { "Message", "Welcome!" }
        }
    },
    new TaskScheduleOptions { Name = "Send Email Task", Delay = TimeSpan.FromSeconds(2) }
);
#endregion

#region Задача с повтором
Console.WriteLine("Планирование задачи ProcessOrderTask с повтором...");
var taskId2 = await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(ProcessOrderTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object>
        {
            { "OrderId", "12345" },
            { "Amount", 99.99m }
        }
    },
    new TaskScheduleOptions
    {
        Name = "Process Order Task",
        MaxRetries = 2,
        Delay = TimeSpan.FromSeconds(3)
    }
);
#endregion

#region Отмена задачи
Console.WriteLine("Планирование задачи, которую можно отменить...");
var taskId3 = await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(LongRunningTask).AssemblyQualifiedName!,
        Args = []
    },
    new TaskScheduleOptions { Name = "Long Running Task", Delay = TimeSpan.FromSeconds(1) }
);
_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    var cancelled = await scheduler.CancelTaskAsync(taskId3);
    Console.WriteLine($"Задача {taskId3} отменена: {cancelled}");
});
#endregion

#region Планирование 100 задач
Console.WriteLine("Планирование 100 задач");
var tasks = new List<Task>();

for (int i = 0; i < 100; i++)
{
    var task = scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(SimpleTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object> { { "Id", i } }
        },
        new TaskScheduleOptions
        {
            Name = $"Simple Task {i}",
            Delay = TimeSpan.FromMilliseconds(i % 100)
        }
    );
    tasks.Add(task);
}

await Task.WhenAll(tasks);
Console.WriteLine("Все 100 задач запланированы");
#endregion

#region Примеры с Partition - последовательная обработка заказов
Console.WriteLine("\n=== Примеры с Partition ===");

// Пример 1: Обработка одного заказа с разными этапами в правильном порядке
var orderId = "ORDER-001";
var orderTasks = new List<string>();

// Валидация заказа (первый шаг)
var validationTaskId = await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(OrderValidationTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object>
        {
            { "OrderId", orderId },
            { "Step", "Validation" }
        }
    },
    new TaskScheduleOptions
    {
        Name = $"Validate {orderId}",
        PartitionKey = orderId,        // Важно: одинаковый PartitionKey
        OrderIndex = 1,               // Важно: порядковый индекс
        Delay = TimeSpan.FromSeconds(1)
    });
orderTasks.Add(validationTaskId);

// Обработка оплаты (второй шаг - после валидации)
var paymentTaskId = await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(PaymentProcessingTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object>
        {
            { "OrderId", orderId },
            { "Step", "Payment" },
            { "Amount", 199.99m }
        }
    },
    new TaskScheduleOptions
    {
        Name = $"Process Payment for {orderId}",
        PartitionKey = orderId,       // Важно: тот же PartitionKey
        OrderIndex = 2,              // Важно: следующий индекс
        Delay = TimeSpan.FromSeconds(2),
        DependsOnTaskId = validationTaskId  // Зависимость от валидации
    });
orderTasks.Add(paymentTaskId);

// Обновление инвентаря (третий шаг - после оплаты)
var inventoryTaskId = await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(InventoryUpdateTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object>
        {
            { "OrderId", orderId },
            { "Step", "Inventory Update" },
            { "ProductId", "PROD-123" },
            { "Quantity", 1 }
        }
    },
    new TaskScheduleOptions
    {
        Name = $"Update Inventory for {orderId}",
        PartitionKey = orderId,       // Важно: тот же PartitionKey
        OrderIndex = 3,              // Важно: последний индекс
        Delay = TimeSpan.FromSeconds(3),
        DependsOnTaskId = paymentTaskId  // Зависимость от оплаты
    });
orderTasks.Add(inventoryTaskId);

Console.WriteLine($"Запланирована последовательная обработка заказа {orderId}:");
Console.WriteLine($"- Валидация: {validationTaskId}");
Console.WriteLine($"- Оплата: {paymentTaskId}");
Console.WriteLine($"- Инвентарь: {inventoryTaskId}");
#endregion

#region Примеры с Partition - параллельная обработка разных заказов
Console.WriteLine("\nПланирование обработки нескольких заказов параллельно...");

var orderIds = new[] { "ORDER-002", "ORDER-003", "ORDER-004", "ORDER-005" };
var allOrderTasks = new List<Task<string>>();

foreach (var order in orderIds)
{
    // Каждый заказ обрабатывается в своей партиции, но этапы внутри заказа последовательны
    var validateTask = scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(OrderValidationTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "OrderId", order },
                { "Step", "Validation" }
            }
        },
        new TaskScheduleOptions
        {
            Name = $"Validate {order}",
            PartitionKey = order,      // Разные PartitionKey - обрабатываются параллельно
            OrderIndex = 1,
            Delay = TimeSpan.FromSeconds(1)
        });

    var processPaymentTask = scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(PaymentProcessingTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "OrderId", order },
                { "Step", "Payment" },
                { "Amount", 299.99m }
            }
        },
        new TaskScheduleOptions
        {
            Name = $"Process Payment for {order}",
            PartitionKey = order,      // Разные PartitionKey
            OrderIndex = 2,
            Delay = TimeSpan.FromSeconds(2),
            DependsOnTaskId = (await validateTask) // Зависимость от валидации
        });

    var updateInventoryTask = scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(InventoryUpdateTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "OrderId", order },
                { "Step", "Inventory Update" },
                { "ProductId", $"PROD-{order.Substring(6)}" },
                { "Quantity", 1 }
            }
        },
        new TaskScheduleOptions
        {
            Name = $"Update Inventory for {order}",
            PartitionKey = order,      // Разные PartitionKey
            OrderIndex = 3,
            Delay = TimeSpan.FromSeconds(3),
            DependsOnTaskId = (await processPaymentTask) // Зависимость от оплаты
        });

    allOrderTasks.Add(validateTask);
    allOrderTasks.Add(processPaymentTask);
    allOrderTasks.Add(updateInventoryTask);
}

var allOrderTaskIds = await Task.WhenAll(allOrderTasks);
Console.WriteLine($"Запланировано {allOrderTaskIds.Length} задач для {orderIds.Length} заказов");
#endregion

#region Примеры с Partition - обработка уведомлений пользователя последовательно
Console.WriteLine("\nПланирование последовательной обработки уведомлений для пользователя...");

var userId = "USER-123";
var notificationTasks = new List<string>();

for (int i = 1; i <= 5; i++)
{
    var notificationTaskId = await scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(UserNotificationTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "UserId", userId },
                { "NotificationId", $"NOTIF-{i:D2}" },
                { "Message", $"Message {i} for user {userId}" }
            }
        },
        new TaskScheduleOptions
        {
            Name = $"Notification {i} for {userId}",
            PartitionKey = userId,     // Все уведомления для одного пользователя в одной партиции
            OrderIndex = i,            // Порядок важен
            Delay = TimeSpan.FromSeconds(i) // Разные задержки
        });
    notificationTasks.Add(notificationTaskId);
}

Console.WriteLine($"Запланировано {notificationTasks.Count} уведомлений для пользователя {userId}, которые будут обработаны последовательно");
#endregion

#region Примеры с Partition - сравнение с отсутствием Partition
Console.WriteLine("\nСравнение: задачи с Partition vs без Partition...");

// Задачи БЕЗ Partition - будут выполняться параллельно
var noPartitionTasks = new List<Task<string>>();
for (int i = 0; i < 3; i++)
{
    noPartitionTasks.Add(scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(SimpleTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "Id", $"NO_PART_{i}" },
                { "Description", "Without partition - runs in parallel" }
            }
        },
        new TaskScheduleOptions
        {
            Name = $"No Partition Task {i}",
            Delay = TimeSpan.FromSeconds(1)
        }));
}

// Задачи С Partition - будут выполняться последовательно
var partitionTasks = new List<Task<string>>();
var sharedPartitionKey = "SHARED_PARTITION";
for (int i = 0; i < 3; i++)
{
    partitionTasks.Add(scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(SimpleTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "Id", $"WITH_PART_{i}" },
                { "Description", "With partition - runs sequentially" }
            }
        },
        new TaskScheduleOptions
        {
            Name = $"Partition Task {i}",
            PartitionKey = sharedPartitionKey,
            OrderIndex = i,                
            Delay = TimeSpan.FromSeconds(1)
        }));
}

var noPartResults = await Task.WhenAll(noPartitionTasks);
var partResults = await Task.WhenAll(partitionTasks);

Console.WriteLine("Задачи БЕЗ Partition (могут выполняться параллельно):");
noPartResults.ToList().ForEach(id => Console.WriteLine($"- {id}"));

Console.WriteLine("Задачи С Partition (выполняются последовательно):");
partResults.ToList().ForEach(id => Console.WriteLine($"- {id}"));
#endregion

#region Примеры с Partition - обработка заказов с использованием ProcessOrderTask
Console.WriteLine("\nПланирование обработки заказов с использованием ProcessOrderTask...");

var processOrderTasks = new List<string>();
for (int i = 1; i <= 5; i++)
{
    var orderId_Partition = $"ORD-{i:D3}";
    var processTaskId = await scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(ProcessOrderTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "OrderId", orderId_Partition },
                { "Amount", (i * 100m) }
            }
        },
        new TaskScheduleOptions
        {
            Name = $"Process Order {orderId_Partition}",
            PartitionKey = "ORDER_PROCESSING",  
            OrderIndex = i,                     
            Delay = TimeSpan.FromSeconds(i)
        });
    processOrderTasks.Add(processTaskId);
}

Console.WriteLine($"Запланировано {processOrderTasks.Count} задач обработки заказов в одной партиции:");
processOrderTasks.ForEach(id => Console.WriteLine($"- {id}"));
#endregion

var hostTask = host.RunAsync();

await hostTask;