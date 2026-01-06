using InMemoryTaskQueue.Factories;
using InMemoryTaskQueue.Handlers;
using InMemoryTaskQueue.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InMemoryTaskQueue.Tests
{
    public class DefaultTaskFactoryTests
    {
        /// <summary>
        /// Проверяет, что фабрика может создать Func из TaskDefinition, если TaskType зарегистрирован в DI.
        /// </summary>
        [Fact]
        public void CreateTask_ReturnsFuncFromServiceProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITaskHandler, TestTaskHandler>(); 
            services.AddSingleton<TestTaskHandler>();
            var provider = services.BuildServiceProvider();

            var factory = new DefaultTaskFactory(provider);

            var taskDef = new TaskDefinition
            {
                TaskType = typeof(TestTaskHandler).AssemblyQualifiedName!,
                Args = new Dictionary<string, object>()
            };

            // Act
            var func = factory.CreateTask(taskDef);

            // Assert
            Assert.NotNull(func);
        }

        /// <summary>
        /// Проверяет, что фабрика выбрасывает исключение, если TaskType не найден или не реализует ITaskHandler.
        /// </summary>
        [Fact]
        public void CreateTask_ThrowsIfHandlerNotFound()
        {
            // Arrange
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();

            var factory = new DefaultTaskFactory(provider);

            var taskDef = new TaskDefinition
            {
                TaskType = "NonExistentType",
                Args = new Dictionary<string, object>()
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => factory.CreateTask(taskDef));
        }
    }

    // Вспомогательный класс для теста
    public class TestTaskHandler : ITaskHandler
    {
        public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
