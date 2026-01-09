using InMemoryTaskQueue.Models;
using InMemoryTaskQueue.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InMemoryTaskQueue.Tests
{
    public class InMemoryTaskStoreTests
    {


        /// <summary>
        /// Проверяет, что Dequeue возвращает задачу, срок которой истёк.
        /// Ожидаемый результат: возвращается задача с DueTime <= DateTime.UtcNow.
        /// </summary>
        [Fact]
        public async Task DequeueDueTaskAsync_ReturnsDueTask()
        {
            // Arrange
            var store = new InMemoryTaskStore();
            var dueTask = new QueuedTask { DueTime = DateTime.UtcNow.AddMinutes(-1) };
            var futureTask = new QueuedTask { DueTime = DateTime.UtcNow.AddMinutes(1) };

            await store.EnqueueAsync(dueTask);
            await store.EnqueueAsync(futureTask);

            // Act
            var result = await store.DequeueDueTaskAsync();

            // Assert
            Assert.Equal(dueTask.Id, result?.Id);
        }
    }
}
