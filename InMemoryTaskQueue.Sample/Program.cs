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
        Args = new Dictionary<string, object>()
    },
    new TaskScheduleOptions { Name = "Long Running Task", Delay = TimeSpan.FromSeconds(1) }
);
_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    var cancelled = await scheduler.CancelTaskAsync(taskId3);
    Console.WriteLine($"Задача {taskId3} отменена: {cancelled}");
});

var hostTask = host.RunAsync();
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
Console.WriteLine("Все 1000 задач запланированы");
#endregion

await hostTask;