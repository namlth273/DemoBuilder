using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ĐemoMultiThread
{
    public class Publisher
    {
        public Publisher(ServiceFactory serviceFactory)
        {
            PublishStrategies[PublishStrategy.Async] = new CustomMediator(serviceFactory, AsyncContinueOnException);
            PublishStrategies[PublishStrategy.ParallelNoWait] = new CustomMediator(serviceFactory, ParallelNoWait);
            PublishStrategies[PublishStrategy.ParallelWhenAll] = new CustomMediator(serviceFactory, ParallelWhenAll);
            PublishStrategies[PublishStrategy.ParallelWhenAny] = new CustomMediator(serviceFactory, ParallelWhenAny);
            PublishStrategies[PublishStrategy.SyncContinueOnException] = new CustomMediator(serviceFactory, SyncContinueOnException);
            PublishStrategies[PublishStrategy.SyncStopOnException] = new CustomMediator(serviceFactory, SyncStopOnException);
        }

        public IDictionary<PublishStrategy, IMediator> PublishStrategies = new Dictionary<PublishStrategy, IMediator>();
        public PublishStrategy DefaultStrategy { get; set; } = PublishStrategy.SyncContinueOnException;

        public Task Publish<TNotification>(TNotification notification) where TNotification : INotification
        {
            return Publish(notification, DefaultStrategy, default(CancellationToken));
        }

        public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy) where TNotification : INotification
        {
            return Publish(notification, strategy, default(CancellationToken));
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken) where TNotification : INotification
        {
            return Publish(notification, DefaultStrategy, cancellationToken);
        }

        public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken cancellationToken) where TNotification : INotification
        {
            if (!PublishStrategies.TryGetValue(strategy, out var mediator))
            {
                throw new ArgumentException($"Unknown strategy: {strategy}");
            }

            return mediator.Publish(notification, cancellationToken);
        }

        private Task ParallelWhenAll(IEnumerable<Func<Task>> handlers)
        {
            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                tasks.Add(Task.Run(() => handler()));
            }

            return Task.WhenAll(tasks);
        }

        private Task ParallelWhenAny(IEnumerable<Func<Task>> handlers)
        {
            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                tasks.Add(Task.Run(() => handler()));
            }

            return Task.WhenAny(tasks);
        }

        private Task ParallelNoWait(IEnumerable<Func<Task>> handlers)
        {
            foreach (var handler in handlers)
            {
                Task.Run(() => handler());
            }

            return Task.CompletedTask;
        }

        private async Task AsyncContinueOnException(IEnumerable<Func<Task>> handlers)
        {
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            foreach (var handler in handlers)
            {
                try
                {
                    tasks.Add(handler());
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
                {
                    exceptions.Add(ex);
                }
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                exceptions.AddRange(ex.Flatten().InnerExceptions);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
            {
                exceptions.Add(ex);
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        private async Task SyncStopOnException(IEnumerable<Func<Task>> handlers)
        {
            foreach (var handler in handlers)
            {
                await handler().ConfigureAwait(false);
            }
        }

        private async Task SyncContinueOnException(IEnumerable<Func<Task>> handlers)
        {
            var exceptions = new List<Exception>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler().ConfigureAwait(false);
                }
                catch (AggregateException ex)
                {
                    exceptions.AddRange(ex.Flatten().InnerExceptions);
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}