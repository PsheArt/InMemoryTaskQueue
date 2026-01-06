using Moq;
using Microsoft.Extensions.Hosting;
using Xunit;
using Microsoft.Extensions.Logging;
using InMemoryTaskQueue.Processors;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Options;
using InMemoryTaskQueue.Stores;
using InMemoryTaskQueue.Services;

namespace InMemoryTaskQueue.Tests
{
    public class TaskProcessorTests
    {
        [Fact]
        public async Task ExecuteAsync_ProcessesDueTask()
        {
            var mockStore = new Mock<ITaskStore>();
            var mockExecutor = new Mock<ITaskExecutor>();
            var logger = new Mock<ILogger<TaskProcessor>>();
            var options = new InMemoryTaskQueueOptions { PollingInterval = TimeSpan.FromMilliseconds(10) };

            var dueTask = new QueuedTask { DueTime = DateTime.UtcNow.AddMinutes(-1) };
            var cts = new CancellationTokenSource();

            mockStore.SetupSequence(s => s.DequeueDueTaskAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(dueTask)
                     .ReturnsAsync((QueuedTask?)null);

            var processor = new TaskProcessor(mockStore.Object,mockExecutor.Object,logger.Object, Microsoft.Extensions.Options.Options.Create(options));

            await processor.StartAsync(cts.Token);
            await Task.Delay(30);
            await processor.StopAsync(cts.Token);

            mockExecutor.Verify(e => e.ExecuteAsync(dueTask, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
