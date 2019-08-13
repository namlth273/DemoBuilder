using MediatR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ĐemoMultiThread
{
    public class TestWorkerPool
    {
        public class Command : INotification
        {
            public Guid Id { get; set; }
        }

        public class AddTaskNotificationHandler : INotificationHandler<Command>
        {
            private readonly TaskPool _taskPool;

            public AddTaskNotificationHandler(IMemoryCache cache)
            {
                cache.TryGetValue("TaskPool", out _taskPool);
            }

            public async Task Handle(Command notification, CancellationToken cancellationToken)
            {
                do
                {
                    var random = new Random().Next(2, 5);
                    await _taskPool.EnqueueAsync(LongRunningTask, true);
                    Console.WriteLine("1 task enqueue | Count " + _taskPool.DefaultQueue.Count + " | Random delay time " + random);
                    await Task.Delay(TimeSpan.FromSeconds(random), cancellationToken);
                } while (true);
            }

            private async Task LongRunningTask()
            {
                var random = new Random().Next(1, 3);

                var builder = new StringBuilder("Thread Id " + Thread.CurrentThread.ManagedThreadId.ToString("00"))
                    .Append(" | ")
                    .Append("Long task running... | RandomNumber " + random)
                    .Append(" | ")
                    .Append(DateTime.Now)
                    .Append(" | ")
                    .Append("Done with no enqueue. Queue count " + _taskPool.DefaultQueue.Count);

                await Task.Delay(TimeSpan.FromSeconds(2));

                Console.WriteLine(builder.ToString());
            }
        }

        public class NotificationHandler : INotificationHandler<Command>
        {
            private readonly TaskPool _taskPool;
            private readonly CancellationTokenSource _source;

            public NotificationHandler(IMemoryCache cache)
            {
                cache.TryGetValue("TaskPool", out _taskPool);

                _source = new CancellationTokenSource();

                _taskPool.Completed += (sender, args) =>
                {
                    _source.Cancel();
                };
            }

            public async Task Handle(Command notification, CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                do
                {
                    await _taskPool.StartTaskAsync();

                    await RunUntilCancellation(async () =>
                    {
                        Console.WriteLine("There is no task in queue, this process will be sleep in 5 seconds before try dequeuing again... Thread Id " + Thread.CurrentThread.ManagedThreadId);
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }, _source.Token);

                } while (true);
            }

            //public async Task AddTasks()
            //{
            //    await _taskPool.EnqueueAsync(LongRunningTask);

            //    await Task.Delay(TimeSpan.FromSeconds(5));
            //}

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

            //private async Task LongRunningTask()
            //{
            //    var random = new Random().Next(1, 3);

            //    var builder = new StringBuilder("Thread Id " + Thread.CurrentThread.ManagedThreadId.ToString("00"))
            //        .Append(" | ")
            //        .Append("Long task running... | RandomNumber " + random)
            //        .Append(" | ")
            //        .Append(DateTime.Now)
            //        .Append(" | ");

            //    if (random == 2)`
            //    {
            //        await _taskPool.EnqueueAsync(LongRunningTask);
            //        await _taskPool.EnqueueAsync(LongRunningTask);
            //        await _taskPool.EnqueueAsync(LongRunningTask);
            //        builder.Append("Done with enqueue   . Queue count " + _taskPool.DefaultQueue.Count);
            //    }
            //    else
            //        builder.Append("Done with no enqueue. Queue count " + _taskPool.DefaultQueue.Count);

            //    await Task.Delay(TimeSpan.FromSeconds(1));

            //    Console.WriteLine(builder.ToString());
            //}
        }
    }
}