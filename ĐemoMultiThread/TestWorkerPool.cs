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

                _taskPool.Available += async (sender, args) =>
                {
                    var random = new Random().Next(1, 3);

                    var builder = new StringBuilder()
                        .AppendStartDate()
                        .AppendWithSeparator("There is 1 slot available...............................")
                        .AppendCount(_taskPool);

                    await Task.Delay(TimeSpan.FromSeconds(random));

                    if (random == 1)
                        await _taskPool.EnqueueAsync(LongRunningTask);

                    if (_taskPool.WorkingTasks.Count < 3)
                    {
                        do
                        {
                            await _taskPool.EnqueueAsync(LongRunningTask);
                        } while (_taskPool.WorkingTasks.Count < 6);
                    }

                    builder
                        .AppendStartDate()
                        .AppendWithSeparator("1 task enqueue")
                        .AppendWithSeparator("Random delay time " + random)
                        .AppendWithSeparator("End Date " + DateTime.Now.ToString("mm:ss:fff"))
                        .AppendCount(_taskPool);

                    Console.WriteLine(builder.ToString());
                };
            }

            public async Task Handle(Command notification, CancellationToken cancellationToken)
            {
                do
                {
                    var random = new Random().Next(1, 2);

                    if (_taskPool.DefaultQueue.Count == 6)
                    {
                        break;

                        //await Task.Delay(TimeSpan.FromSeconds(random), cancellationToken);

                        //continue;
                    }

                    await _taskPool.EnqueueAsync(LongRunningTask, true);

                    var builder = new StringBuilder()
                        .AppendStartDate()
                        .AppendWithSeparator("1 task enqueue")
                        .AppendWithSeparator("Random delay time " + random)
                        .AppendWithSeparator("End Date " + DateTime.Now.ToString("mm:ss:fff"))
                        .AppendCount(_taskPool);

                    Console.WriteLine(builder.ToString());

                    await Task.Delay(TimeSpan.FromSeconds(random), cancellationToken);
                } while (true);
            }

            private async Task LongRunningTask()
            {
                var random = new Random().Next(5, 12);

                var builder = new StringBuilder()
                    .AppendStartDate()
                    .AppendWithSeparator("Thread Id " + Thread.CurrentThread.ManagedThreadId.ToString("00"))
                    .AppendWithSeparator("Long task running...............................")
                    .AppendWithSeparator("Sleep time " + random);

                await Task.Delay(TimeSpan.FromSeconds(random));

                builder
                    .AppendWithSeparator("End Date " + DateTime.Now.ToString("mm:ss:fff"))
                    .AppendCount(_taskPool);

                Console.WriteLine(builder.ToString());
            }
        }

        public class NotificationHandler : INotificationHandler<Command>
        {
            private readonly TaskPool _taskPool;
            private CancellationTokenSource _source;

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
                await Task.Delay(TimeSpan.FromSeconds(7), cancellationToken);

                do
                {
                    var id = Guid.NewGuid();

                    var builder = new StringBuilder()
                        .AppendStartDate()
                        .AppendWithSeparator("StartTaskAsync")
                        .AppendWithSeparator("Id " + id)
                        .AppendCount(_taskPool);

                    Console.WriteLine(builder.ToString());

                    await _taskPool.StartTaskAsync();

                    await RunUntilCancellation(async () =>
                    {
                        var runBuilder = new StringBuilder()
                            .AppendStartDate()
                            .AppendWithSeparator("There is no task in queue")
                            .AppendWithSeparator("Thread Id " + Thread.CurrentThread.ManagedThreadId)
                            .AppendWithSeparator("Id " + id)
                            .AppendCount(_taskPool);

                        Console.WriteLine(runBuilder.ToString());
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                        _source = new CancellationTokenSource();
                    }, _source.Token);

                } while (true);
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

    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendWithSeparator(this StringBuilder builder, string value)
        {
            builder.Append(value).Append(" | ");
            return builder;
        }

        public static StringBuilder AppendCount(this StringBuilder builder, TaskPool taskPool)
        {
            builder.AppendWithSeparator("Queue count " + taskPool.DefaultQueue.Count)
                .AppendWithSeparator("WorkingTasks count " + taskPool.WorkingTasks.Count);
            return builder;
        }

        public static StringBuilder AppendStartDate(this StringBuilder builder)
        {
            builder.AppendWithSeparator("Time " + DateTime.Now.ToString("mm:ss:fff"));
            return builder;
        }
    }
}