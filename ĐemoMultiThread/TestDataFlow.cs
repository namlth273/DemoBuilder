using MediatR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ĐemoMultiThread
{
    public class TestDataFlow
    {
        public class Command : INotification
        {
            public Guid Id { get; set; }
        }

        public class NotificationHandler : INotificationHandler<Command>
        {
            public async Task Handle(Command notification, CancellationToken cancellationToken)
            {
                var lockObject = new object();
                var lockObject2 = new object();

                var now = DateTime.Now;

                var items = new ConcurrentQueue<Request>
                (
                    new List<Request>
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
                    }
                );

                var poolBlock = new TransformBlock<Request, Response>(async request =>
                {
                    var builder = new StringBuilder()
                        .AppendStartDate()
                        .AppendWithSeparator($"Enter Pool Block");

                    var response = await ProcessRequest(request);

                    builder.AppendWithSeparator($"{request.Id.ToString().Substring(2, 3)}");
                    builder.AppendWithSeparator($"{request.MessageCount:00}");
                    builder.AppendWithSeparator($"{response.RequestType}");
                    builder.AppendWithSeparator($"Delay {response.Delay}");

                    Console.WriteLine(builder.ToString());

                    request.LastProcessedDate = DateTime.Now;
                    request.MessageCount -= 1;

                    if (request.MessageCount > 0)
                        items.Enqueue(request);

                    return response;
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 3
                });

                var addToPoolBlock = new ActionBlock<Response>(response =>
                {
                    lock (lockObject)
                    {
                        items.TryDequeue(out var nextRequest);

                        if (nextRequest != null)
                        {
                            poolBlock.Post(nextRequest);
                        }

                        //if (runningItems.Count == 0 && items.IsEmpty)
                        //poolBlock.Complete();
                    }
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 1
                });

                var mainBlock = new TransformBlock<Request, bool>(async item =>
                {
                    var builder = new StringBuilder()
                        .AppendStartDate()
                        .AppendWithSeparator($"Enter Main Block");

                    Console.WriteLine(builder.ToString());

                    items.TryDequeue(out var request);
                    poolBlock.Post(request);
                    items.TryDequeue(out var request2);
                    poolBlock.Post(request2);
                    items.TryDequeue(out var request3);
                    poolBlock.Post(request3);

                    await poolBlock.Completion;

                    builder.AppendWithSeparator($"EndDate {DateTime.Now:mm:ss:fff}");

                    Console.WriteLine(builder.ToString());

                    return true;
                });

                var broadcastBlock = new BroadcastBlock<Request>(null, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 1
                });

                var endMainBlock = new ActionBlock<bool>(request =>
                {
                    //mainBlock.Complete();
                    //broadcastBlock.Complete();
                });

                //mainBlock.LinkTo(endMainBlock);
                poolBlock.LinkTo(addToPoolBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true
                });
                broadcastBlock.LinkTo(mainBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true
                });

                broadcastBlock.Post(new Request { RequestType = "request type 1" });

                //poolBlock.Complete();
                //mainBlock.Complete();
                broadcastBlock.Complete();

                await poolBlock.Completion;
                await mainBlock.Completion;
                await broadcastBlock.Completion;

                Console.WriteLine("Done");
            }

            private async Task<Response> ProcessRequest(Request request)
            {
                var random = new Random().Next(2, 5);

                await Task.Delay(TimeSpan.FromSeconds(2));
                return new Response
                {
                    Id = request.Id,
                    RequestType = request.RequestType,
                    Delay = random
                };
            }

            public class Request
            {
                public Guid Id { get; set; }
                public string RequestType { get; set; }
                public DateTime LastProcessedDate { get; set; }
                public int MessageCount { get; set; }
                public bool IsProcessing { get; set; }
            }

            public class Response
            {
                public Guid Id { get; set; }
                public string RequestType { get; set; }
                public bool HasError { get; set; }
                public DateTime LastProcessedDate { get; set; }
                public int Delay { get; set; }
            }
        }
    }
}
