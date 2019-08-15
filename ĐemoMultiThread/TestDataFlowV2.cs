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
    public class TestDataFlowV2
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
                    new Request
                    {
                        Id = Guid.NewGuid(),
                        RequestType = "Request Type 3 ===",
                        MessageCount = 3,
                        LastProcessedDate = now,
                    },
                    new Request
                    {
                        Id = Guid.NewGuid(),
                        RequestType = "Request Type 4 ====",
                        MessageCount = 13,
                        LastProcessedDate = now,
                    },
                };
            }

            #endregion

            public async Task<Unit> Handle(Command notification, CancellationToken cancellationToken)
            {
                var count = 0;
                var actionBlocks = new List<TransformBlock<Request, Request>>();
                var canStop = false;
                var broadcastBlock = new BroadcastBlock<Request>(null);
                var bufferBlock = new BufferBlock<Request>();
                var bufferBlock2 = new BufferBlock<Request>();
                var actionBlock2 = new ActionBlock<Request>(async request =>
                {
                    count++;

                    new StringBuilder()
                        .AppendStartDate()
                        .AppendWithSeparator($"Count {count:00}".PadRight(11))
                        .WriteLine();

                    if (count == 4)
                    {
                        canStop = true;

                        new StringBuilder()
                            .AppendStartDate()
                            .AppendWithSeparator($"Can Stop")
                            .AppendEndDate()
                            .WriteLine();
                    }

                    if (!canStop)
                    {
                        var canSend = await broadcastBlock.SendAsync(request, cancellationToken);

                        //new StringBuilder()
                        //    .AppendStartDate()
                        //    .AppendWithSeparator($"CanSend {canSend}")
                        //    .WriteLine();
                    }
                });

                var transformBlock =
                    new TransformBlock<Request, Request>(request => request, _executionOptions);

                var transformBlock2 =
                    new TransformBlock<Request, Request>(request => request, _executionOptions);

                for (int i = 0; i < 2; i++)
                {
                    var actionBlock = new TransformBlock<Request, Request>(async request =>
                    {
                        await ProcessRequest(request);

                        //new StringBuilder()
                        //    .AppendStartDate()
                        //    .AppendWithSeparator("ActionBlock")
                        //    .AppendWithSeparator($"Id {request.Id.ToString().Substring(0, 3)}")
                        //    .AppendWithSeparator($"Type {request.RequestType.PadRight(18)}")
                        //    .AppendEndDate()
                        //    .WriteLine();

                        if (count > 4)
                        {
                            new StringBuilder()
                                .AppendStartDate()
                                .AppendWithSeparator($"Stop")
                                .AppendEndDate()
                                .WriteLine();

                            broadcastBlock.Complete();
                        }

                        return request;
                    }, new ExecutionDataflowBlockOptions
                    {
                        BoundedCapacity = 1
                    });

                    actionBlock.LinkTo(bufferBlock2, _options);
                    bufferBlock.LinkTo(actionBlock, _options);
                    actionBlocks.Add(actionBlock);
                }

                bufferBlock2.LinkTo(actionBlock2, _options);
                transformBlock.LinkTo(bufferBlock, _options);
                broadcastBlock.LinkTo(transformBlock, _options);

                broadcastBlock.Post(_items[0]);
                broadcastBlock.Post(_items[1]);
                broadcastBlock.Post(_items[2]);

                //await actionBlock2.Completion;

                await Task.WhenAll(actionBlocks.Select(s => s.Completion));

                new StringBuilder()
                    .AppendStartDate()
                    .AppendWithSeparator("Done".PadRight(11))
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
                    .AppendWithSeparator($"Type {request.RequestType.PadRight(18)}");

                var random = new Random().Next(5, 18);

                await Task.Delay(TimeSpan.FromSeconds(random));

                builder
                    .AppendWithSeparator($"Delay {random}")
                    .AppendEndDate()
                    .WriteLine();

                request.LastProcessedDate = DateTime.Now;

                return request;
            }

            public class Request
            {
                public Guid Id { get; set; }
                public string RequestType { get; set; }
                public DateTime LastProcessedDate { get; set; }
                public int MessageCount { get; set; }
                public bool IsProcessing { get; set; }
            }

            #endregion
        }
    }
}
