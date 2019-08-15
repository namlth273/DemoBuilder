using MediatR;
using System;
using System.Collections.Generic;
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
                var bufferBlock = new BufferBlock<Request>();

                var transformBlock = new TransformBlock<Request, Request>(async request => await ProcessRequest(request), _executionOptions);

                //var actionBlock = new ActionBlock<Request>(ProcessRequest, _executionOptions);

                var step2TransformBlock = new TransformBlock<Request, Request>(request => request, _executionOptions);

                var step2BufferBlock = new BufferBlock<Request>();

                var step3ActionBlock = new ActionBlock<Request>(async request =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    var builder = new StringBuilder()
                        .AppendStartDate()
                        .AppendWithSeparator("Step 3")
                        .AppendWithSeparator($"Id {request.Id.ToString().Substring(0, 3)}")
                        .AppendWithSeparator($"Type {request.RequestType}")
                        .AppendWithSeparator($"End {DateTime.Now:mm:ss:fff}")
                        .WriteLine();
                });

                //step2BufferBlock.LinkTo(step3ActionBlock, _options);
                step2TransformBlock.LinkTo(step2BufferBlock, _options);
                transformBlock.LinkTo(step2TransformBlock, _options);
                bufferBlock.LinkTo(transformBlock, _options);

                bufferBlock.Post(_items[0]);
                bufferBlock.Post(_items[1]);
                bufferBlock.Post(_items[2]);

                //bufferBlock.Complete();

                await bufferBlock.Completion;

                Console.WriteLine("Done");

                return Unit.Value;
            }

            #region Private

            private async Task<Request> ProcessRequest(Request request)
            {
                var random = new Random().Next(2, 5);

                await Task.Delay(TimeSpan.FromSeconds(1));

                var builder = new StringBuilder()
                    .AppendStartDate()
                    .AppendWithSeparator($"Id {request.Id.ToString().Substring(0, 3)}")
                    .AppendWithSeparator($"Type {request.RequestType}")
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
