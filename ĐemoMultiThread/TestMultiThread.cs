using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ĐemoMultiThread
{
    public class TestMultiThread
    {
        public class Command : INotification
        {
            public CancellationTokenSource TokenSource { get; set; }
            public Guid Id { get; set; }
        }

        public class SaveMessageNotificationHandler : INotificationHandler<Command>
        {
            public async Task Handle(Command notification, CancellationToken cancellationToken)
            {
                cancellationToken.Register(() =>
                {
                    Console.WriteLine("================SaveMessageNotificationHandler cancelling...");
                });

                var count = 0;
                do
                {
                    count++;

                    Console.WriteLine("Saving message... " + count + " | ThreadId " + Thread.CurrentThread.ManagedThreadId);
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

                    if (count == 5)
                    {
                        notification.TokenSource.Cancel();
                    }
                } while (true);
            }
        }

        public class UpdateMessageNotificationHandler : INotificationHandler<Command>
        {
            public async Task Handle(Command notification, CancellationToken cancellationToken)
            {
                cancellationToken.Register(() =>
                {
                    Console.WriteLine("================UpdateMessageNotificationHandler cancelling...");
                });

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                var count = 1;
                do
                {
                    Console.WriteLine("Updating message... " + count + " | ThreadId " + Thread.CurrentThread.ManagedThreadId);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    count++;
                } while (true);
            }
        }
    }
}