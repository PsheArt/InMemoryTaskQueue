# InMemoryTaskQueue

**Production-ready, DI-friendly, тестируемая, расширяемая библиотека** для планирования асинхронных задач в .NET.

## Особенности

- **In-Memory очередь задач** — хранение задач в памяти.
- **Планирование задач** с отсрочкой, таймаутом, повторами.
- **Отмена задач по ID**.
- **Расширяемая архитектура** — можно подключить Redis, PostgreSQL, логгирование, метрики.
- **Партиционирование задач** — задачи с одинаковым `PartitionKey` выполняются последовательно.

## Структура проекта

- InMemoryTaskQueue.sln
  - src/
    - InMemoryTaskQueue/
      - Models/
      - Stores/
      - Services/
      - Strategies/
      - Cancellation/
      - Extensions/
      - InMemoryTaskQueue.csproj
  - tests/
    - InMemoryTaskQueue.Tests/
      - InMemoryTaskQueue.Tests.csproj
  - samples/
    - InMemoryTaskQueue.Sample/
      - InMemoryTaskQueue.Sample.csproj

## Интерфейсы

| Интерфейс                  | Назначение                     |
|----------------------------|--------------------------------|
| `ITaskScheduler`           | Планирование задач.            |
| `ITaskExecutor`            | Выполнение задач.              |
| `ITaskStore`               | Хранение задач.                |
| `IRetryStrategy`           | Стратегия повтора.             |
| `ITaskFactory`             | Восстановление задач.          |
| `ITaskCancellationRegistry`| Отмена задач.                  |
| `ITaskHandler`             | Обработчик задачи.             |


## Установка

```bash
dotnet add package InMemoryTaskQueue
```

## Примеры
### Регистрация обработчика задачи
```csharp
public class SendEmailTask : ITaskHandler
{
    public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
    {
        var email = args["Email"] as string;
        // Отправить email
        return Task.CompletedTask;
    }
}

// В Program.cs
builder.Services.AddSingleton<ITaskHandler, SendEmailTask>();
builder.Services.AddSingleton<SendEmailTask>(); // Добавить и сам тип
```

### Планирование задачи через ITaskHandler
```csharp
await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(SendEmailTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object>
        {
            { "Email", "user@example.com" },
            { "Message", "Hello!" }
        }
    },
    new TaskScheduleOptions
    {
        Name = "Send Email Task",
        Delay = TimeSpan.FromSeconds(5)
    }
);
```
### Задача с повтором
```csharp
await scheduler.ScheduleTaskAsync(
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
        MaxRetries = 3,
        RetryStrategy = new ExponentialBackoffRetryStrategy()
    }
);
```

### Отмена задачи
```csharp
var taskId = await scheduler.ScheduleTaskAsync(...);
await scheduler.CancelTaskAsync(taskId);
```

## Пример с внешними системами
### Redis [RedisTaskStore](InMemoryTaskQueue.Sample/Tasks/RedisTaskStore.cs)
### SQL [SQLTaskStore](InMemoryTaskQueue.Sample/Tasks/SQLTaskStore.cs)
### Kafka [KafkaTaskStore](InMemoryTaskQueue.Sample/Tasks/KafkaTaskStore.cs)

## Сценарии использования

### Отложенные уведомления
- **Сценарий:** Отправка email/SMS уведомлений через 5 минут после регистрации.  
- **Пример:** 
```csharp
public class SendWelcomeEmailTask : ITaskHandler
{
    public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
    {
        var email = args["Email"] as string;
        var userId = args["UserId"] as string;

        // Отправка email
        Console.WriteLine($"Отправка приветственного письма на {email} для пользователя {userId}");
        return Task.CompletedTask;
    }
}

// В сервисе регистрации
await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(SendWelcomeEmailTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object>
        {
            { "Email", "user@example.com" },
            { "UserId", "12345" }
        }
    },
    new TaskScheduleOptions { Delay = TimeSpan.FromMinutes(5) }
);
```

### Повторяющиеся задачи с экспоненциальной задержкой
- **Сценарий:** Повторная попытка отправки данных в API, если произошёл сбой.  
- **Пример:** 
```csharp
public class SendDataToApiTask : ITaskHandler
{
    public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
    {
        var endpoint = args["Endpoint"] as string;
        var data = args["Data"] as string;

        // Отправка данных
        await HttpClient.PostAsync(endpoint, new StringContent(data), cancellationToken);
    }
}

await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(SendDataToApiTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object>
        {
            { "Endpoint", "https://api.example.com/data" },
            { "Data", "{ \"key\": \"value\" }" }
        }
    },
    new TaskScheduleOptions
    {
        MaxRetries = 3,
        RetryStrategy = new ExponentialBackoffRetryStrategy()
    }
);
```

### Обработка заказов, платежей, инвойсов
- **Сценарий:** После создания заказа — обновить сток, отправить уведомление, выставить счёт.  
- **Пример:** 
```csharp
public class ProcessOrderTask : ITaskHandler
{
    public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
    {
        var orderId = args["OrderId"] as string;
        // Обработка заказа
        return Task.CompletedTask;
    }
}

public class UpdateStockTask : ITaskHandler
{
    public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
    {
        var productId = args["ProductId"] as string;
        // Обновление стока
        return Task.CompletedTask;
    }
}

public class SendInvoiceTask : ITaskHandler
{
    public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
    {
        var orderId = args["OrderId"] as string;
        // Отправка счёта
        return Task.CompletedTask;
    }
}

await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(ProcessOrderTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object> { { "OrderId", "12345" } }
    }
);

await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(UpdateStockTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object> { { "ProductId", "ABC123" } }
    }
);

await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(SendInvoiceTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object> { { "OrderId", "12345" } }
    }
);
```

### Отмена задач
- **Сценарий:** Отменить отправку email, если пользователь отписался.  
- **Пример:** 
```csharp
var taskId = await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(SendEmailTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object> { { "Email", "user@example.com" } }
    }
);

// Позже, если пользователь отписался
await scheduler.CancelTaskAsync(taskId);
```

### Очистка кэша, файлов, логов
- **Сценарий:** Удалить временные файлы через 1 час после загрузки.  
- **Пример:** 
```csharp
public class CleanupFilesTask : ITaskHandler
{
    public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
    {
        var filePath = args["FilePath"] as string;
        File.Delete(filePath);
        return Task.CompletedTask;
    }
}

await scheduler.ScheduleTaskAsync(
    new TaskDefinition
    {
        TaskType = typeof(CleanupFilesTask).AssemblyQualifiedName!,
        Args = new Dictionary<string, object> { { "FilePath", "/temp/file.txt" } }
    },
    new TaskScheduleOptions { Delay = TimeSpan.FromHours(1) }
);
```

### Обработка событий (Event-Driven Architecture)
- **Сценарий:** После события `UserRegistered` выполнить `SendWelcomeEmail`, `CreateProfile`, `LogActivity`.  
- **Пример:** 
```csharp
public class UserRegisteredEvent
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
}

// В обработчике события
public async Task HandleUserRegistered(UserRegisteredEvent @event)
{
    await scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(SendWelcomeEmailTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "Email", @event.Email },
                { "UserId", @event.UserId }
            }
        }
    );

    await scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(CreateProfileTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object> { { "UserId", @event.UserId } }
        }
    );

    await scheduler.ScheduleTaskAsync(
        new TaskDefinition
        {
            TaskType = typeof(LogActivityTask).AssemblyQualifiedName!,
            Args = new Dictionary<string, object>
            {
                { "UserId", @event.UserId },
                { "Action", "UserRegistered" }
            }
        }
    );
}
```
