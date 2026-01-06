using InMemoryTaskQueue.Cancellation;
using InMemoryTaskQueue.Factories;
using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InMemoryTaskQueue.Tests
{
    public class DefaultTaskExecutorTests
    {
        /// <summary>
        /// Проверяет, что при выполнении задачи вызывается ITaskFactory.CreateTask и затем сама задача.
        /// Ожидаемый результат: задача выполнена, ExecuteAsync вызван.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_CallsTaskFactoryAndExecutesTask()
        {
            // Arrange
            var mockFactory = new Mock<ITaskFactory>();
            var mockCancellationRegistry = new Mock<ITaskCancellationRegistry>();
            var logger = new Mock<ILogger<DefaultTaskExecutor>>();

            // Передаём null вместо meterFactory
            var taskDefinition = new TaskDefinition { TaskType = "TestTask", Args = new Dictionary<string, object>() };
            var queuedTask = new QueuedTask { TaskDefinition = taskDefinition, CancellationSourceId = "test123" };

            var executed = false;
            Func<CancellationToken, Task> mockFunc = (CancellationToken ct) =>
            {
                executed = true;
                return Task.CompletedTask;
            };

            mockFactory.Setup(f => f.CreateTask(taskDefinition)).Returns(mockFunc);

            var executor = new DefaultTaskExecutor(mockFactory.Object, mockCancellationRegistry.Object, logger.Object, null);

            // Act
            await executor.ExecuteAsync(queuedTask, CancellationToken.None);

            // Assert
            Assert.True(executed);
        }

        /// <summary>
        /// Проверяет, что при превышении ExecutionTimeout задача завершается с TimeoutException.
        /// Ожидаемый результат: задача не завершена, выбрасывается исключение.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_HandlesTimeout()
        {
            // Arrange
            var mockFactory = new Mock<ITaskFactory>();
            var mockCancellationRegistry = new Mock<ITaskCancellationRegistry>();
            var logger = new Mock<ILogger<DefaultTaskExecutor>>();

            var taskDefinition = new TaskDefinition { TaskType = "TestTask", Args = new Dictionary<string, object>() };
            var queuedTask = new QueuedTask
            {
                TaskDefinition = taskDefinition,
                CancellationSourceId = "test123",
                ExecutionTimeout = TimeSpan.FromMilliseconds(10)
            };

            mockFactory.Setup(f => f.CreateTask(taskDefinition)).Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(100, ct); // Дольше таймаута
            });

            var executor = new DefaultTaskExecutor(mockFactory.Object, mockCancellationRegistry.Object, logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() => executor.ExecuteAsync(queuedTask, CancellationToken.None));
        }
    }
}
