using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ĐemoMultiThread
{
    public class TestDataFlowV3
    {
        public class Command : IRequest<Unit>
        {
            public Guid Id { get; set; }
        }

        public class NotificationHandler : IRequestHandler<Command>
        {
            #region Ctor

            private readonly List<Request> _items;
            private readonly DataflowLinkOptions _options;
            private readonly ExecutionDataflowBlockOptions _executionOptions;
            private const int MaxParallelCount = 3;

            public NotificationHandler()
            {
                _options = new DataflowLinkOptions
                {
                    PropagateCompletion = true
                };

                _executionOptions = new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = MaxParallelCount
                };

                var now = DateTime.Now;

                _items = new List<Request>
                {
                    new Request
                    {
                        Id = Guid.NewGuid(),
                        RequestType = "Request Type 1 =",
                        MessageCount = 5,
                        LastProcessedDate = now,
                    },
                    new Request
                    {
                        Id = Guid.NewGuid(),
                        RequestType = "Request Type 2 ==",
                        MessageCount = 7,
                        LastProcessedDate = now,
                    },
                    //new Request
                    //{
                    //    Id = Guid.NewGuid(),
                    //    RequestType = "Request Type 3 ===",
                    //    MessageCount = 3,
                    //    LastProcessedDate = now,
                    //},
                    //new Request
                    //{
                    //    Id = Guid.NewGuid(),
                    //    RequestType = "Request Type 4 ====",
                    //    MessageCount = 13,
                    //    LastProcessedDate = now,
                    //},
                    //new Request
                    //{
                    //    Id = Guid.NewGuid(),
                    //    RequestType = "Request Type 5 =====",
                    //    MessageCount = 10,
                    //    LastProcessedDate = now,
                    //},
                    //new Request
                    //{
                    //    Id = Guid.NewGuid(),
                    //    RequestType = "Request Type 6 ======",
                    //    MessageCount = 15,
                    //    LastProcessedDate = now,
                    //},
                    //new Request
                    //{
                    //    Id = Guid.NewGuid(),
                    //    RequestType = "Request Type 7 =======",
                    //    MessageCount = 15,
                    //    LastProcessedDate = now,
                    //},
                };
            }

            #endregion

            public async Task<Unit> Handle(Command notification, CancellationToken cancellationToken)
            {
                //var count = 0;
                //var bufferBlock = new BufferBlock<Request>(new ExecutionDataflowBlockOptions
                //{
                //    MaxDegreeOfParallelism = MaxParallelCount
                //});

                //var firstBlock = new TransformBlock<Request, Request>(async request =>
                //{
                //    await ProcessRequest(request);

                //    return request;
                //}, new ExecutionDataflowBlockOptions
                //{
                //    MaxDegreeOfParallelism = MaxParallelCount,
                //    EnsureOrdered = true
                //});

                //var secondBlock = new ActionBlock<Request>(async request =>
                //{
                //    count++;

                //    var builder = new StringBuilder()
                //        .AppendStartDate()
                //        .AppendWithSeparator($"Count {count:00}".PadRight(11))
                //        .AppendWithSeparator($"Id {request.Id.ToString().Substring(0, 3)}");

                //    if (request.MessageCount > 0)
                //    {
                //        var canSend = await firstBlock.SendAsync(request, cancellationToken);
                //        builder.AppendWithSeparator($"CanSend {canSend}");
                //    }
                //    else
                //    {
                //        builder.AppendWithSeparator($"MessageCount {request.MessageCount}")
                //            .AppendWithSeparator($"PendingCount {_items.Where(c => c.MessageCount > 0).Sum(s => s.MessageCount)}")
                //            .AppendWithSeparator($"FirstBlockCount {firstBlock.InputCount}");

                //        if (firstBlock.InputCount == 0 && !_items.Any(c => c.MessageCount > 0))
                //        {
                //            firstBlock.Complete();
                //            builder.AppendWithSeparator("Complete()");
                //        }
                //    }

                //    builder.WriteLine();
                //});

                //firstBlock.LinkTo(bufferBlock, _options);

                //bufferBlock.LinkTo(secondBlock, _options);

                //foreach (var request in _items)
                //{
                //    firstBlock.Post(request);
                //}

                //await firstBlock.Completion;

                var workerPool = new WorkerPool<Request>(MaxParallelCount, ProcessRequest, _items);

                var isDone = await workerPool.HandleAsync();

                new StringBuilder()
                    .AppendStartDate()
                    .AppendWithSeparator($"isDone {isDone}")
                    .AppendWithSeparator($"PendingCount {_items.Count(w => w.MessageCount > 0)}")
                    .WriteLine();

                return Unit.Value;
            }

            #region Private

            private async Task<Request> ProcessRequest(Request request)
            {
                var builder = new StringBuilder()
                    .AppendStartDate()
                    .AppendWithSeparator("Process".PadRight(11))
                    .AppendWithSeparator($"Id {request.Id.ToString().Substring(0, 3)}")
                    .AppendWithSeparator($"MessageCount {request.MessageCount:00}")
                    .AppendWithSeparator($"Type {request.RequestType.PadRight(22)}");

                var random = new Random().Next(5, 18);

                await Task.Delay(TimeSpan.FromSeconds(random));

                builder
                    .AppendWithSeparator($"Delay {random}")
                    .AppendEndDate()
                    .WriteLine();

                request.LastProcessedDate = DateTime.Now;
                request.MessageCount--;

                return request;
            }

            public class Request : ISubscriptionInfo
            {
                public Guid Id { get; set; }
                public bool IsPaused { get; set; }
                public string RequestType { get; set; }
                public DateTime LastProcessedDate { get; set; }
                public int MessageCount { get; set; }
                public bool IsProcessing { get; set; }
            }

            #endregion
        }

        public class WorkerPool<T> where T : ISubscriptionInfo
        {
            private readonly Func<T, Task> _handleMessage;
            private readonly List<T> _queue = new List<T>();
            private TransformBlock<T, T> _firstBlock;
            private BufferBlock<T> _bufferBlock;
            private ActionBlock<T> _lastBlock;
            private readonly ExecutionDataflowBlockOptions _executionBlockOptions;
            private readonly DataflowLinkOptions _linkOptions;
            private int _proceededMessageCount = 0;

            public WorkerPool(int maxParallelCount, Func<T, Task> handleMessage, IEnumerable<T> queueItems = null)
            {
                _handleMessage = handleMessage;
                if (queueItems != null)
                    _queue.AddRange(queueItems);
                _executionBlockOptions = new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxParallelCount
                };
                _linkOptions = new DataflowLinkOptions
                {
                    PropagateCompletion = true
                };

                InitBlocks();
            }

            private void InitBlocks()
            {
                _firstBlock = new TransformBlock<T, T>(async request =>
                {
                    await _handleMessage(request);
                    return request;
                }, _executionBlockOptions);

                _bufferBlock = new BufferBlock<T>(_executionBlockOptions);

                _lastBlock = new ActionBlock<T>(async request =>
                {
                    _proceededMessageCount++;

                    var builder = new StringBuilder()
                        .AppendStartDate()
                        .AppendWithSeparator($"Count {_proceededMessageCount:00}".PadRight(11))
                        .AppendWithSeparator($"Id {request.Id.ToString().Substring(0, 3)}");

                    if (request.MessageCount > 0)
                    {
                        var canSend = await _firstBlock.SendAsync(request);
                        builder.AppendWithSeparator($"CanSend {canSend}");
                    }
                    else
                    {
                        builder.AppendWithSeparator($"MessageCount {request.MessageCount}")
                            .AppendWithSeparator($"PendingCount {_queue.Where(c => c.MessageCount > 0).Sum(s => s.MessageCount)}")
                            .AppendWithSeparator($"FirstBlockCount {_firstBlock.InputCount}");

                        if (_firstBlock.InputCount == 0 && !_queue.Any(c => c.MessageCount > 0))
                        {
                            _firstBlock.Complete();
                            builder.AppendWithSeparator("Complete()");
                        }
                    }

                    builder.WriteLine();
                });

                _firstBlock.LinkTo(_bufferBlock, _linkOptions);
                _bufferBlock.LinkTo(_lastBlock, _linkOptions);
            }

            public async Task<bool> HandleAsync()
            {
                var items = _queue.Where(w => !w.IsPaused).Where(w => w.MessageCount > 0);

                foreach (var message in items)
                {
                    _firstBlock.Post(message);
                }

                await _firstBlock.Completion;

                return true;
            }
        }

        public interface ISubscriptionInfo
        {
            Guid Id { get; set; }
            bool IsPaused { get; set; }
            int MessageCount { get; set; }
        }
    }
}
