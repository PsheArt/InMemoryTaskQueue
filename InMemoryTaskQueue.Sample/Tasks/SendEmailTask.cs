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
}
