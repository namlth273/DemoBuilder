using Autofac;
using MediatR;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ĐemoMultiThread
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<AutofacModule>();

            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var publisher = scope.Resolve<Publisher>();

                var source = new CancellationTokenSource();

                var changeToken = new CancellationChangeToken(source.Token);

                //var compositeChangeToken =
                //    new CompositeChangeToken(
                //        new List<IChangeToken>
                //        {
                //            changeToken
                //        });

                //compositeChangeToken.RegisterChangeCallback(Console.WriteLine, source);

                publisher.Publish(new TestMultiThread.SendMessage
                {
                    TokenSource = source,
                    Id = Guid.NewGuid()
                }, PublishStrategy.ParallelWhenAll, source.Token);

                Console.WriteLine("Press any key to cancel tasks");
                Console.ReadLine();
                Console.WriteLine(source.IsCancellationRequested);
                Console.ReadLine();
            }
        }
    }

    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.AddMediatR(ThisAssembly);
            builder.RegisterType<Publisher>().SingleInstance();
        }
    }

    public class TestMultiThread
    {
        public class SendMessage : INotification
        {
            public CancellationTokenSource TokenSource { get; set; }
            public Guid Id { get; set; }
        }

        public class SaveMessageNotificationHandler : INotificationHandler<SendMessage>
        {
            public async Task Handle(SendMessage notification, CancellationToken cancellationToken)
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

        public class UpdateMessageNotificationHandler : INotificationHandler<SendMessage>
        {
            public async Task Handle(SendMessage notification, CancellationToken cancellationToken)
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
