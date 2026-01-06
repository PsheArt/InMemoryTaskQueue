using InMemoryTaskQueue.Cancellation;
using InMemoryTaskQueue.Handlers;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Services;
using InMemoryTaskQueue.Stores;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using TaskScheduler = InMemoryTaskQueue.Services.TaskScheduler;

namespace InMemoryTaskQueue.Tests
{
    public class TaskSchedulerTests
    {
        /// <summary>
        /// Проверяет, что при планировании задачи через ITaskHandler она добавляется в хранилище.
        /// Ожидаемый результат: вызывается метод EnqueueAsync у ITaskStore.
        /// </summary>
        [Fact]
        public async Task ScheduleTaskAsync_CreatesTaskInStore()
        {
            // Arrange
            var mockStore = new Mock<ITaskStore>();
            var mockCancellationRegistry = new Mock<ITaskCancellationRegistry>();
            var logger = new Mock<ILogger<TaskScheduler>>();

            var scheduler = new TaskScheduler(mockStore.Object, mockCancellationRegistry.Object, logger.Object);

            var taskDef = new TaskDefinition
            {
                TaskType = typeof(TestTaskHandler).AssemblyQualifiedName!,
                Args = new Dictionary<string, object>()
            };

            // Act
            var taskId = await scheduler.ScheduleTaskAsync(taskDef);

            // Assert
            Assert.NotNull(taskId);
            mockStore.Verify(s => s.EnqueueAsync(It.IsAny<QueuedTask>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Проверяет, что при отмене задачи вызывается ITaskCancellationRegistry.Cancel.
        /// Ожидаемый результат: возвращается true, и вызывается метод Cancel.
        /// </summary>
        [Fact]
        public async Task CancelTaskAsync_CallsCancellationRegistry()
        {
            // Arrange
            var mockStore = new Mock<ITaskStore>();
            var mockCancellationRegistry = new Mock<ITaskCancellationRegistry>();
            var logger = new Mock<ILogger<TaskScheduler>>();

            mockCancellationRegistry.Setup(c => c.Cancel("task123")).Returns(true);

            var scheduler = new TaskScheduler(mockStore.Object, mockCancellationRegistry.Object, logger.Object);

            // Act
            var result = await scheduler.CancelTaskAsync("task123");

            // Assert
            Assert.True(result);
            mockCancellationRegistry.Verify(c => c.Cancel("task123"), Times.Once);
        }
    }

}
