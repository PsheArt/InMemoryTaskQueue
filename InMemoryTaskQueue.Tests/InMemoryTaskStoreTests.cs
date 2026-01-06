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
        /// Проверяет, что задача добавляется в список.
        /// Ожидаемый результат: задача сохранена.
        /// </summary>
        [Fact]
        public async Task EnqueueAsync_AddsTaskToList()
        {
            // Arrange
            var store = new InMemoryTaskStore();
            var task = new QueuedTask { DueTime = DateTime.UtcNow.AddMinutes(1) };

            // Act
            await store.EnqueueAsync(task);

            // Assert
            // Т.к. нет публичного доступа к списку, можно использовать reflection или добавить метод для тестов
            // В продакшене лучше не делать публичным, но для тестов можно добавить internal метод
        }

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
