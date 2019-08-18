using AutoFixture;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ĐemoMultiThread
{
    public class WorkerPoolV2<T> where T : Message
    {
        public delegate WorkerPool<T> Factory(int maxParallelCount, Func<T, Task> handleMessage,
            IEnumerable<T> queueItems = null);

        private readonly Func<T, Task> _handleMessage;
        private readonly IMapper _mapper;
        private readonly int _maxParallelCount;
        private readonly List<T> _queue = new List<T>();
        private readonly ExecutionDataflowBlockOptions _executionBlockOptions;
        private readonly DataflowLinkOptions _linkOptions;
        private int _proceededMessageCount = 0;
        private readonly IServiceBusClient _client;
        private readonly IFixture _fixture;

        private BroadcastBlock<WorkerPoolMessage<T>> _broadcastBlock;
        private BufferBlock<WorkerPoolMessage<T>> _bufferCountBlock;
        private TransformBlock<WorkerPoolMessage<T>, WorkerPoolMessage<T>> _handleMessageBlock;
        private TransformBlock<WorkerPoolMessage<T>, WorkerPoolMessage<T>> _processCountBlock;
        private BufferBlock<WorkerPoolMessage<T>> _bufferSubscriptionBlock;
        private TransformBlock<WorkerPoolMessage<T>, WorkerPoolMessage<T>> _getNextSubscriptionBlock;
        private TransformBlock<WorkerPoolMessage<T>, WorkerPoolMessage<T>> _updateSubscriptionBlock;
        private BroadcastBlock<Task<WorkerPoolMessage<T>>> _intervalBlock;
        private ActionBlock<WorkerPoolMessage<T>> _endBlock;
        private readonly IList<ISubscriptionInfoV2> _subscriptions;
        private bool _hasCompleted = false;

        public WorkerPoolV2(IMapper mapper, int maxParallelCount, Func<T, Task> handleMessage, IEnumerable<T> queueItems = null)
        {
            _client = new ServiceBusClient();
            _fixture = new Fixture();
            _subscriptions = new List<ISubscriptionInfoV2>();

            _handleMessage = handleMessage;
            _mapper = mapper;
            _maxParallelCount = maxParallelCount;
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
            _broadcastBlock = new BroadcastBlock<WorkerPoolMessage<T>>(message => message);
            _endBlock = new ActionBlock<WorkerPoolMessage<T>>(message =>
            {
                _hasCompleted = !_hasCompleted;
                new StringBuilder().AppendStartDate().AppendWithSeparator("Ending...")
                    .AppendWithSeparator($"HasCompleted {_hasCompleted}")
                    .WriteLine();
                _broadcastBlock.Complete();
            });
            _bufferCountBlock = new BufferBlock<WorkerPoolMessage<T>>(_executionBlockOptions);
            _handleMessageBlock = new TransformBlock<WorkerPoolMessage<T>, WorkerPoolMessage<T>>(async message =>
            {
                var response = _mapper.Map<Message>(message.Message);

                response.IsGetNextSubscription = true;

                await _handleMessage(message.Message);

                new StringBuilder()
                    .AppendStartDate()
                    .AppendWithSeparator($"HandleMessageBlock")
                    .AppendWithSeparator($"Sub {response.Subscription.Substring(response.Subscription.Length - 3, 3)}")
                    .AppendWithSeparator($"Next? {response.IsGetNextSubscription}")
                    .AppendWithSeparator($"Update? {response.IsUpdateSubscription}")
                    //.WriteLine()
                    ;

                message.Message = response as T;

                return message;
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _maxParallelCount
            });
            _processCountBlock = new TransformBlock<WorkerPoolMessage<T>, WorkerPoolMessage<T>>(async message =>
            {
                //TODO implement process count
                var builder = new StringBuilder().AppendStartDate().AppendWithSeparator("ProcessCountBlock");

                var response = _mapper.Map<WorkerPoolMessage<T>>(message);

                if (message.NextStep == EnumNextStep.GetNextMessage)
                {
                    _proceededMessageCount--;
                    message.NextStep = EnumNextStep.GetNextMessage;
                    builder.AppendWithSeparator($"Count -- {_proceededMessageCount}");
                }
                else
                {
                    _proceededMessageCount++;
                    message.NextStep = EnumNextStep.UpdateMessage;
                    builder.AppendWithSeparator($"Count ++ {_proceededMessageCount}");
                }

                builder
                    .AppendWithSeparator($"Sub {response.Message.Subscription.Substring(response.Message.Subscription.Length - 3, 3)}")
                    .AppendWithSeparator($"Next? {response.Message.IsGetNextSubscription}")
                    .AppendWithSeparator($"Update? {response.Message.IsUpdateSubscription}")
                    .WriteLine()
                    ;

                return response;
            });
            _bufferSubscriptionBlock = new BufferBlock<WorkerPoolMessage<T>>(_executionBlockOptions);
            _updateSubscriptionBlock = new TransformBlock<WorkerPoolMessage<T>, WorkerPoolMessage<T>>(message =>
            {
                //TODO implement update subscription
                var subscription =
                    _subscriptions.FirstOrDefault(w => w.SubscriptionType == message.Message.Subscription);

                if (subscription != null)
                {
                    subscription.UndeliveredMessageCount--;

                    new StringBuilder().AppendStartDate()
                        .AppendWithSeparator($"UpdateSubscriptionBlock")
                        .AppendWithSeparator(
                            $"Sub {message.Message.Subscription.Substring(message.Message.Subscription.Length - 3, 3)}")
                        .AppendWithSeparator($"UndeliveredMessageCount {subscription.UndeliveredMessageCount}")
                        //.WriteLine()
                        ;
                }

                return message;
            });
            _getNextSubscriptionBlock = new TransformBlock<WorkerPoolMessage<T>, WorkerPoolMessage<T>>(message =>
            {
                //TODO implement get next subscription
                var subscription = _subscriptions
                    .Where(w => !w.IsPaused)
                    .FirstOrDefault(w => w.UndeliveredMessageCount > 0);

                if (subscription != null)
                {
                    new StringBuilder().AppendStartDate()
                        .AppendWithSeparator($"GetNextBlock")
                        .AppendWithSeparator(
                            $"Sub {message.Message.Subscription.Substring(message.Message.Subscription.Length - 3, 3)}")
                        //.WriteLine()
                        ;

                    var nextMessage = new WorkerPoolMessage<Message>
                    {
                        Message = new Message
                        {
                            Subscription = subscription.SubscriptionType,
                        },
                        NextStep = EnumNextStep.GetNextMessage
                    };

                    return nextMessage as WorkerPoolMessage<T>;
                }
                //else
                //{
                //    new StringBuilder().AppendStartDate().AppendWithSeparator("No subscription found")
                //        .AppendWithSeparator($"_proceededMessageCount {_proceededMessageCount}")
                //        .WriteLine();
                //}

                return null;
            });
            _intervalBlock = new BroadcastBlock<Task<WorkerPoolMessage<T>>>(async message =>
            {
                //TODO delay 30s
                await Task.Delay(TimeSpan.FromSeconds(1));

                //TODO implement interval to get subscription from API
                new StringBuilder().AppendStartDate().AppendWithSeparator($"Interval").WriteLine();

                return await message;
            });

            _intervalBlock.LinkTo(_intervalBlock);
            //_intervalBlock.LinkTo(_bufferSubscriptionBlock);
            // order matter for _getNextSubscriptionBlock
            _getNextSubscriptionBlock.LinkTo(DataflowBlock.NullTarget<WorkerPoolMessage<T>>(), _linkOptions, message => _hasCompleted);
            _getNextSubscriptionBlock.LinkTo(DataflowBlock.NullTarget<WorkerPoolMessage<T>>(), _linkOptions, message => message == null & _proceededMessageCount > 0);
            _getNextSubscriptionBlock.LinkTo(_endBlock, _linkOptions, message => !_hasCompleted && message == null);
            _getNextSubscriptionBlock.LinkTo(_broadcastBlock, _linkOptions);
            _bufferSubscriptionBlock.LinkTo(_getNextSubscriptionBlock, _linkOptions, message => message.NextStep == EnumNextStep.GetNextMessage);
            _bufferSubscriptionBlock.LinkTo(_updateSubscriptionBlock, _linkOptions, message => message.NextStep == EnumNextStep.UpdateMessage);
            _processCountBlock.LinkTo(_bufferSubscriptionBlock, _linkOptions);
            _bufferCountBlock.LinkTo(_processCountBlock, _linkOptions);
            _handleMessageBlock.LinkTo(_bufferCountBlock, _linkOptions);
            _broadcastBlock.LinkTo(_bufferCountBlock, _linkOptions);
            _broadcastBlock.LinkTo(_handleMessageBlock, _linkOptions);
        }

        public async Task<bool> StartAsync()
        {
            var response = await _client.GetSubscription("");

            _subscriptions.Add(response);

            var message = new Message
            {
                Subscription = response.SubscriptionType,
                RealmId = "shared",
            };

            var workerPoolMessage = new WorkerPoolMessage<T>
            {
                Message = message as T
            };

            _broadcastBlock.Post(workerPoolMessage);

            await _broadcastBlock.Completion;

            return true;
        }
    }

    public class WorkerPoolMessage<T> where T : IMessage
    {
        public T Message { get; set; }
        public EnumNextStep? NextStep { get; set; }
        public bool HasCompleted { get; set; }
    }

    public enum EnumNextStep
    {
        GetNextMessage,
        UpdateMessage,
        UpdateMessageInterval
    }
}
