using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
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

    public class TestWorkerPool
    {
        public class Command : INotification
        {
            public Guid Id { get; set; }
        }

        public class NotificationHandler : INotificationHandler<Command>
        {
            private readonly TaskPool _taskPool;
            private readonly CancellationTokenSource _source;

            public NotificationHandler()
            {
                _taskPool = new TaskPool(2, new List<Func<Task>>
                {
                    LongRunningTask,
                    LongRunningTask,
                    LongRunningTask,
                    LongRunningTask,
                    LongRunningTask,
                });

                _source = new CancellationTokenSource();

                _taskPool.Completed += (sender, args) =>
                {
                    _source.Cancel();
                };
            }

            public async Task Handle(Command notification, CancellationToken cancellationToken)
            {
                await AddTasks();

                await RunUntilCancellation(async () =>
                {
                    Console.WriteLine("RunUntilCancellation Task running...");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    Console.WriteLine("Handle finished");
                }, _source.Token);
            }

            public async Task AddTasks()
            {
                await _taskPool.Enqueue(LongRunningTask);

                await Task.Delay(TimeSpan.FromSeconds(5));
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

            private async Task LongRunningTask()
            {
                var random = new Random().Next(1, 3);

                var builder = new StringBuilder("Thread Id " + Thread.CurrentThread.ManagedThreadId.ToString("00"))
                    .Append(" | ")
                    .Append("Long task running... | RandomNumber " + random)
                    .Append(" | ")
                    .Append(DateTime.Now)
                    .Append(" | ");

                if (random == 2)
                {
                    await _taskPool.Enqueue(LongRunningTask);
                    await _taskPool.Enqueue(LongRunningTask);
                    await _taskPool.Enqueue(LongRunningTask);
                    builder.Append("Done with enqueue   . Queue count " + _taskPool.DefaultQueue.Count);
                }
                else
                    builder.Append("Done with no enqueue. Queue count " + _taskPool.DefaultQueue.Count);

                await Task.Delay(TimeSpan.FromSeconds(1));

                Console.WriteLine(builder.ToString());
            }
        }
    }
}