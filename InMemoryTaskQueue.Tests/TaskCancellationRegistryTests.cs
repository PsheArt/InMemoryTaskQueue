using InMemoryTaskQueue.Cancellation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InMemoryTaskQueue.Tests
{
    public class TaskCancellationRegistryTests
    {
        /// <summary>
        /// Проверяет, что токен регистрируется по ID.
        /// Ожидаемый результат: возвращается CancellationTokenSource.
        /// </summary>
        [Fact]
        public void Register_AddsToken()
        {
            // Arrange
            var registry = new TaskCancellationRegistry();

            // Act
            var cts = registry.Register("task123");

            // Assert
            Assert.NotNull(cts);
            Assert.False(cts.Token.IsCancellationRequested);
        }

        /// <summary>
        /// Проверяет, что токен отменяется по ID.
        /// Ожидаемый результат: возвращается true, токен отменён.
        /// </summary>
        [Fact]
        public void Cancel_TokenIsCancelled()
        {
            // Arrange
            var registry = new TaskCancellationRegistry();
            var cts = registry.Register("task123");

            // Act
            var result = registry.Cancel("task123");

            // Assert
            Assert.True(result);
            Assert.True(cts.Token.IsCancellationRequested);
        }

        /// <summary>
        /// Проверяет, что токен удаляется и освобождается.
        /// Ожидаемый результат: токен больше не доступен.
        /// </summary>
        [Fact]
        public void Unregister_DisposesToken()
        {
            // Arrange
            var registry = new TaskCancellationRegistry();
            var cts = registry.Register("task123");

            // Act
            registry.Unregister("task123");

            // Assert
            Assert.Throws<ObjectDisposedException>(() => cts.Token.ThrowIfCancellationRequested());
        }
    }
}
