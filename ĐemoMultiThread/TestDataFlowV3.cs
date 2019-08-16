﻿using AutoFixture;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

            private readonly IServiceBusClient _client;
            private readonly List<Request> _items;
            private readonly DataflowLinkOptions _options;
            private readonly ExecutionDataflowBlockOptions _executionOptions;
            private const int MaxParallelCount = 3;

            public NotificationHandler(IServiceBusClient client)
            {
                _client = client;
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
                Console.WriteLine(JsonConvert.SerializeObject(await _client.GetSubscriptionInfo(""), Formatting.Indented));
                Console.WriteLine(JsonConvert.SerializeObject(await _client.PullMessage(""), Formatting.Indented));

                //new StringBuilder()
                //    .AppendStartDate()
                //    .AppendWithSeparator($"Start. Pending Count{_items.Count}")
                //    .WriteLine();

                //var workerPool = new WorkerPool<Request>(MaxParallelCount, ProcessRequest, _items);

                //var isDone = await workerPool.StartAsync();

                //new StringBuilder()
                //    .AppendStartDate()
                //    .AppendWithSeparator($"isDone {isDone}")
                //    .AppendWithSeparator($"PendingCount {_items.Count(w => w.MessageCount > 0)}")
                //    .WriteLine();

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

        public interface ISubscriptionInfo
        {
            Guid Id { get; set; }
            bool IsPaused { get; set; }
            int MessageCount { get; set; }
        }

        public interface IServiceBusClient
        {
            Task<SubscriptionInfoV2> GetSubscriptionInfo(string subscription);
            Task<IMessageBody> PullMessage(string subscription);
        }

        public class ServiceBusClient : IServiceBusClient
        {
            private readonly IFixture _fixture;

            public ServiceBusClient()
            {
                _fixture = new Fixture();
            }

            public Task<SubscriptionInfoV2> GetSubscriptionInfo(string subscription)
            {
                return Task.FromResult(new SubscriptionInfoV2
                {
                    SubscriptionType = "test",
                    UndeliveredMessageCount = 6
                });
            }

            public Task<IMessageBody> PullMessage(string subscription)
            {
                return Task.FromResult(_fixture.Create<MessageBody>() as IMessageBody);
            }
        }
    }
}
