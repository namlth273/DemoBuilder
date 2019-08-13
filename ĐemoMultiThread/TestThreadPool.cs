using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ĐemoMultiThread
{
    public class TestThreadPool
    {
        public class Command : INotification
        {
            public Guid Id { get; set; }
        }

        public class NotificationHandler : INotificationHandler<Command>
        {
            public Task Handle(Command notification, CancellationToken cancellationToken)
            {
                Console.WriteLine(Environment.ProcessorCount);
                ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);

                // Get available threads  
                ThreadPool.GetAvailableThreads(out var workers, out var ports);
                Console.WriteLine($"Available worker threads: {workers} ");
                Console.WriteLine($"Available completion port threads: {ports}");

                for (int i = 0; i < 10; i++)
                {
                    ThreadPool.QueueUserWorkItem(BackgroundTaskWithObject, new Person
                    {
                        Name = "Nam Le " + i
                    });
                }

                return Task.CompletedTask;
            }

            void BackgroundTaskWithObject(object stateInfo)
            {
                var data = (Person)stateInfo;
                Console.WriteLine("==================================================================================");
                Console.WriteLine($"Hi {data.Name} from ThreadPool.");

                // Get available threads  
                ThreadPool.GetAvailableThreads(out var workers, out var ports);
                Console.WriteLine($"Available worker threads: {workers} ");
                Console.WriteLine($"Available completion port threads: {ports}");
                Console.WriteLine("==================================================================================");

                Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        public class Person
        {
            public string Name { get; set; }
        }
    }

    public class TestTaskCompletionSource
    {
        public class Command : INotification
        {
            public CancellationTokenSource TokenSource { get; set; }
            public Guid Id { get; set; }
        }

        public class NotificationHandler : INotificationHandler<Command>
        {
            public async Task Handle(Command notification, CancellationToken cancellationToken)
            {
                Console.WriteLine("Task running...");

                await RunUntilCancellation(async () =>
                {
                    Console.WriteLine("RunUntilCancellation Task running...");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }, notification.TokenSource.Token);

                Console.WriteLine("Task completed.");
            }

            public async Task RunUntilCancellation(Func<Task> onCancel, CancellationToken cancellationToken)
            {
                var doneReceiving = new TaskCompletionSource<bool>();

                cancellationToken.Register(
                    async () =>
                    {
                        await onCancel();
                        doneReceiving.SetResult(true); // Signal to quit message listener
                    });

                await doneReceiving.Task.ConfigureAwait(false); // Listen until quit signal is received.
            }
        }
    }
}