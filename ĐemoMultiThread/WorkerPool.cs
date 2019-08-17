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
    public class WorkerPool<T> where T : Message
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

        private BroadcastBlock<T> _broadcastBlock;
        private BufferBlock<T> _bufferCountBlock;
        private TransformBlock<T, T> _handleMessageBlock;
        private TransformBlock<T, T> _processCountBlock;
        private BufferBlock<T> _bufferSubscriptionBlock;
        private TransformBlock<T, T> _getNextSubscriptionBlock;
        private TransformBlock<T, T> _updateSubscriptionBlock;
        private BroadcastBlock<T> _intervalBlock;
        private ActionBlock<T> _endBlock;
        private readonly IList<ISubscriptionInfoV2> _subscriptions;
        private bool _hasCompleted = false;

        public WorkerPool(IMapper mapper, int maxParallelCount, Func<T, Task> handleMessage, IEnumerable<T> queueItems = null)
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
            _broadcastBlock = new BroadcastBlock<T>(message => message);
            _endBlock = new ActionBlock<T>(message =>
            {
                _hasCompleted = !_hasCompleted;
                new StringBuilder().AppendStartDate().AppendWithSeparator("Ending...")
                    .AppendWithSeparator($"HasCompleted {_hasCompleted}")
                    .WriteLine();
                _broadcastBlock.Complete();
            });
            _bufferCountBlock = new BufferBlock<T>(_executionBlockOptions);
            _handleMessageBlock = new TransformBlock<T, T>(async message =>
            {
                var response = _mapper.Map<Message>(message);

                response.IsGetNextSubscription = true;

                await _handleMessage(message);

                new StringBuilder()
                    .AppendStartDate()
                    .AppendWithSeparator($"HandleMessageBlock")
                    .AppendWithSeparator($"Sub {response.Subscription.Substring(response.Subscription.Length - 3, 3)}")
                    .AppendWithSeparator($"Next? {response.IsGetNextSubscription}")
                    .AppendWithSeparator($"Update? {response.IsUpdateSubscription}")
                    //.WriteLine()
                    ;

                return response as T;
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _maxParallelCount
            });
            _processCountBlock = new TransformBlock<T, T>(async message =>
            {

                //TODO implement process count
                var builder = new StringBuilder().AppendStartDate().AppendWithSeparator("ProcessCountBlock");

                var response = _mapper.Map<Message>(message);

                if (message.IsGetNextSubscription)
                {
                    _proceededMessageCount--;
                    response.IsGetNextSubscription = true;
                    builder.AppendWithSeparator($"Count -- {_proceededMessageCount}");
                }
                else
                {
                    _proceededMessageCount++;
                    response.IsUpdateSubscription = true;
                    builder.AppendWithSeparator($"Count ++ {_proceededMessageCount}");
                }

                builder
                    .AppendWithSeparator($"Sub {response.Subscription.Substring(response.Subscription.Length - 3, 3)}")
                    .AppendWithSeparator($"Next? {response.IsGetNextSubscription}")
                    .AppendWithSeparator($"Update? {response.IsUpdateSubscription}")
                    .WriteLine()
                    ;

                return response as T;
            });
            _bufferSubscriptionBlock = new BufferBlock<T>(_executionBlockOptions);
            _updateSubscriptionBlock = new TransformBlock<T, T>(message =>
            {
                //TODO implement update subscription
                var subscription = _subscriptions.FirstOrDefault(w => w.SubscriptionType == message.Subscription);

                if (subscription != null)
                {
                    subscription.UndeliveredMessageCount--;

                    new StringBuilder().AppendStartDate()
                        .AppendWithSeparator($"UpdateSubscriptionBlock")
                        .AppendWithSeparator(
                            $"Sub {message.Subscription.Substring(message.Subscription.Length - 3, 3)}")
                        .AppendWithSeparator($"UndeliveredMessageCount {subscription.UndeliveredMessageCount}")
                        //.WriteLine()
                        ;

                }

                return message;
            });
            _getNextSubscriptionBlock = new TransformBlock<T, T>(message =>
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
                            $"Sub {message.Subscription.Substring(message.Subscription.Length - 3, 3)}")
                        //.WriteLine()
                        ;

                    var nextMessage = new Message
                    {
                        Subscription = subscription.SubscriptionType,
                        HasNextSubscription = true
                    };

                    return nextMessage as T;
                }
                //else
                //{
                //    new StringBuilder().AppendStartDate().AppendWithSeparator("No subscription found")
                //        .AppendWithSeparator($"_proceededMessageCount {_proceededMessageCount}")
                //        .WriteLine();
                //}

                return null;
            });
            _intervalBlock = new BroadcastBlock<T>(async message =>
            {
                //TODO delay 30s
                await Task.Delay(TimeSpan.FromSeconds(1));

                //TODO implement interval to get subscription from API
                new StringBuilder().AppendStartDate().AppendWithSeparator($"Interval").WriteLine();

                return message;
            });

            _intervalBlock.LinkTo(_intervalBlock);
            //_intervalBlock.LinkTo(_bufferSubscriptionBlock);
            // order matter for _getNextSubscriptionBlock
            _getNextSubscriptionBlock.LinkTo(DataflowBlock.NullTarget<T>(), _linkOptions, message => _hasCompleted);
            _getNextSubscriptionBlock.LinkTo(DataflowBlock.NullTarget<T>(), _linkOptions, message => message == null & _proceededMessageCount > 0);
            _getNextSubscriptionBlock.LinkTo(_endBlock, _linkOptions, message => !_hasCompleted && message == null);
            _getNextSubscriptionBlock.LinkTo(_broadcastBlock, _linkOptions, message => !message.IsGetNextSubscription && !message.IsUpdateSubscription && message.HasNextSubscription);
            _getNextSubscriptionBlock.LinkTo(DataflowBlock.NullTarget<T>(), _linkOptions, message => !message.HasNextSubscription);
            _bufferSubscriptionBlock.LinkTo(_getNextSubscriptionBlock, _linkOptions, message => message.IsGetNextSubscription);
            _bufferSubscriptionBlock.LinkTo(_updateSubscriptionBlock, _linkOptions, message => message.IsUpdateSubscription);
            _processCountBlock.LinkTo(_bufferSubscriptionBlock, _linkOptions);
            _bufferCountBlock.LinkTo(_processCountBlock, _linkOptions);
            _handleMessageBlock.LinkTo(_bufferCountBlock, _linkOptions);
            _broadcastBlock.LinkTo(_bufferCountBlock, _linkOptions);
            _broadcastBlock.LinkTo(_handleMessageBlock, _linkOptions);
        }

        public async Task<bool> StartAsync()
        {
            var response = await _client.GetSubscriptionInfo("");

            _subscriptions.Add(response);

            var message = new Message
            {
                Subscription = response.SubscriptionType,
                RealmId = "shared",
            };

            _broadcastBlock.Post(message as T);
            _broadcastBlock.Post(message as T);
            _broadcastBlock.Post(message as T);
            _broadcastBlock.Post(message as T);
            _broadcastBlock.Post(message as T);
            _broadcastBlock.Post(message as T);

            await _broadcastBlock.Completion;

            return true;
        }
    }

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Message, Message>()
                .ForMember(m => m.IsGetNextSubscription, o => o.Ignore())
                .ForMember(m => m.IsUpdateSubscription, o => o.Ignore());
        }
    }
}
