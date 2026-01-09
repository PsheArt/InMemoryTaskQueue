using InMemoryTaskQueue.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryTaskQueue.Sample.Tasks
{
    public class SendEmailTask : ITaskHandler
    {
        public Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var email = args["Email"] as string ?? "unknown";
            var message = args["Message"] as string ?? "No message";

            Console.WriteLine($"Отправка приветственного письма на {email}");
            return Task.CompletedTask;
        }
    }

    public class UserNotificationTask : ITaskHandler
    {
        public async Task ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken)
        {
            var userId = args["UserId"] as string ?? "unknown";
            var notificationId = args["NotificationId"] as string ?? "unknown";
            var message = args["Message"] as string ?? "No message";

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Отправка уведомления {notificationId} пользователю {userId}: {message}");

            // Симуляция отправки уведомления
            await Task.Delay(500, cancellationToken);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Уведомление {notificationId} отправлено пользователю {userId}");
        }
    }
}
