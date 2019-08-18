using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ĐemoMultiThread.WorkerPool
{
    public class WorkerPoolV3
    {
        public delegate WorkerPoolV3 Factory(string subscriptionName, int maxParallelCount);

        private readonly string _subscriptionName;
        private readonly IMapper _mapper;
        private readonly IServiceBusClient _client;

        private int _proceededMessageCount = 0;
        private readonly int _maxParallelCount;
        private readonly IList<RealmInfo> _realmQueue;
        private bool _hasCompleted = false;
        private bool _shouldStopIntervalBlock = false;

        private readonly DataflowLinkOptions _linkOptions;
        private readonly BroadcastBlock<Message> _broadcastBlock;
        private readonly BufferBlock<Message> _bufferCountBlock;
        private readonly TransformBlock<Message, Message> _handleMessageBlock;
        private readonly TransformBlock<Message, Message> _processCountBlock;
        private readonly BufferBlock<Message> _bufferRealmBlock;
        private readonly TransformBlock<Message, Message> _getNextRealmBlock;
        private readonly ActionBlock<Message> _updateRealmBlock;
        private BroadcastBlock<Message> _intervalBlock;
        private readonly ActionBlock<Message> _endBlock;

        public WorkerPoolV3(string subscriptionName, int maxParallelCount, IMapper mapper,
            IDataFlowHandler messageHandler, IServiceBusClient client)
        {
            _subscriptionName = subscriptionName;
            _mapper = mapper;
            _client = client;
            _maxParallelCount = maxParallelCount;
            _realmQueue = new List<RealmInfo>();

            var executionBlockOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxParallelCount
            };
            _linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = true
            };

            _broadcastBlock = new BroadcastBlock<Message>(message => _mapper.Map<Message>(message));

            _bufferCountBlock = new BufferBlock<Message>(executionBlockOptions);

            _handleMessageBlock = new TransformBlock<Message, Message>(async message =>
            {
                var subInfo = new SubscriptionInfo
                {
                    Subscription = _subscriptionName,
                    RealmId = message.RealmId
                };

                await messageHandler.HandleAsync(subInfo);

                message.NextStep = EnumNextStep.GetNextMessage;

                new StringBuilder().AppendStartDate()
                    .AppendWithSeparator($"HandleMessageBlock")
                    .AppendSubscription(_subscriptionName)
                    .AppendNextStep(message)
                    //.WriteLine()
                    ;

                return message;
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _maxParallelCount
            });

            _processCountBlock = new TransformBlock<Message, Message>(message =>
            {
                //TODO implement process count
                var builder = new StringBuilder().AppendStartDate().AppendWithSeparator("ProcessCountBlock");

                if (message.NextStep == EnumNextStep.GetNextMessage)
                {
                    _proceededMessageCount--;
                    builder.AppendWithSeparator($"Count -- {_proceededMessageCount}");
                }
                else
                {
                    _proceededMessageCount++;
                    message.NextStep = EnumNextStep.UpdateMessage;
                    builder.AppendWithSeparator($"Count ++ {_proceededMessageCount}");
                }

                if (_proceededMessageCount < 0)
                {
                    _proceededMessageCount = 0;
                    return null;
                }

                if (_proceededMessageCount > _maxParallelCount)
                {
                    _proceededMessageCount = _maxParallelCount;
                    return null;
                }

                builder.AppendSubscription(_subscriptionName).AppendNextStep(message)
                    .WriteLine()
                    ;

                return message;
            });

            _bufferRealmBlock = new BufferBlock<Message>(executionBlockOptions);

            //_updateRealmBlock = new ActionBlock<Message>(message =>
            //{
            //    //TODO implement update subscription
            //    if (message.NextStep == EnumNextStep.UpdateMessage)
            //    {
            //        var realm =
            //            _realmQueue.FirstOrDefault(w => w.RealmId == message.RealmId && w.Count > 0);

            //        if (realm != null)
            //        {
            //            realm.Count--;

            //            new StringBuilder().AppendStartDate().AppendWithSeparator($"UpdateSubscriptionBlock")
            //                .AppendSubscription(_subscriptionName).AppendWithSeparator($"Realm {realm.RealmId}")
            //                .AppendWithSeparator($"UndeliveredMessageCount {realm.Count}")
            //                .WriteLine()
            //                ;
            //        }
            //    }
            //    else if (message.NextStep == EnumNextStep.UpdateMessageInterval)
            //    {
            //        //TODO call API to get latest sub info
            //        _realmQueue.First().Count += 5;

            //        new StringBuilder().AppendStartDate().AppendWithSeparator($"UpdateSubscriptionBlock")
            //            .AppendSubscription(_subscriptionName).AppendWithSeparator($"UpdateIntervalCount")
            //            .WriteLine()
            //            ;
            //    }
            //    else if (message.NextStep == EnumNextStep.GetNextMessage)
            //    {

            //    }
            //});

            _getNextRealmBlock = new TransformBlock<Message, Message>(message =>
            {
                //TODO implement update subscription
                if (message.NextStep == EnumNextStep.UpdateMessage)
                {
                    var realm =
                        _realmQueue.FirstOrDefault(w => w.RealmId == message.RealmId && w.Count > 0);

                    if (realm != null)
                    {
                        realm.Count--;

                        new StringBuilder().AppendStartDate().AppendWithSeparator($"UpdateMessage")
                            .AppendSubscription(_subscriptionName).AppendWithSeparator($"Realm {realm.RealmId}")
                            .AppendWithSeparator($"UndeliveredMessageCount {realm.Count}")
                            .AppendWithSeparator($"ProcessCount {_proceededMessageCount}")
                            //.WriteLine()
                            ;
                    }

                    return null;
                }

                if (message.NextStep == EnumNextStep.UpdateMessageInterval)
                {
                    //TODO call API to get latest sub info
                    var realm = _realmQueue.First();

                    var random = new Random().Next(1, 1);

                    realm.Count += random;

                    new StringBuilder().WriteLine().AppendStartDate().AppendWithSeparator($"UpdateMessageInterval")
                        .AppendSubscription(_subscriptionName).AppendWithSeparator($"Realm {realm.RealmId}")
                        .AppendWithSeparator($"Add more message {random}/{realm.Count}")
                        .AppendWithSeparator($"ProcessCount {_proceededMessageCount}")
                        .WriteLine()
                        ;

                    if (realm.Count > 0)
                    {
                        var nextMessage = _mapper.Map<Message>(message);

                        for (int i = 1; i <= Math.Min(realm.Count, _maxParallelCount - _proceededMessageCount); i++)
                        {
                            _broadcastBlock.Post(nextMessage);
                        }
                    }
                    else
                    {
                        _shouldStopIntervalBlock = true;
                    }

                    return null;
                }

                if (message.NextStep == EnumNextStep.GetNextMessage)
                {
                    //TODO implement get next subscription
                    var realmId = GetLeastRecentActiveRealmId();

                    var subscription = _realmQueue.FirstOrDefault(w => w.RealmId == realmId && w.Count > 0);

                    if (subscription != null)
                    {
                        new StringBuilder().AppendStartDate().AppendWithSeparator($"GetNextMessage")
                            .AppendSubscription(_subscriptionName).AppendWithSeparator($"Realm {realmId}")
                            .AppendWithSeparator($"ProcessCount {_proceededMessageCount}")
                            //.WriteLine()
                            ;

                        var nextMessage = _mapper.Map<Message>(message);

                        return nextMessage;
                    }
                    //else
                    //{
                    //    new StringBuilder().AppendStartDate().AppendWithSeparator("No subscription found")
                    //        .AppendWithSeparator($"_proceededMessageCount {_proceededMessageCount}")
                    //        .WriteLine();
                    //}
                }

                return null;
            });

            _intervalBlock = new BroadcastBlock<Message>(message =>
            {
                //TODO delay 30s
                Task.Delay(TimeSpan.FromSeconds(7)).Wait();

                if (_hasCompleted || _shouldStopIntervalBlock) return null;

                //TODO implement interval to get subscription from API
                //new StringBuilder().AppendStartDate().AppendWithSeparator($"Interval").WriteLine();

                var response = _mapper.Map<Message>(message);

                response.NextStep = EnumNextStep.UpdateMessageInterval;

                return response;
            });

            _endBlock = new ActionBlock<Message>(message =>
            {
                if (_hasCompleted) return;

                _hasCompleted = !_hasCompleted;
                new StringBuilder().AppendStartDate().AppendWithSeparator("Ending...")
                    .AppendWithSeparator($"HasCompleted {_hasCompleted}").WriteLine();
                _broadcastBlock.Complete();
            });

            LinkBlocks();
        }

        private void LinkBlocks()
        {
            // order matter for _getNextRealmBlock
            _getNextRealmBlock.LinkTo(DataflowBlock.NullTarget<Message>(), _linkOptions, message => message == null && !_shouldStopIntervalBlock);
            _getNextRealmBlock.LinkTo(_endBlock, _linkOptions, message => !_hasCompleted && message == null && _shouldStopIntervalBlock);
            //_getNextRealmBlock.LinkTo(_bufferRealmBlock, _linkOptions, message => message.NextStep == EnumNextStep.GetNextMessage);
            _getNextRealmBlock.LinkTo(_broadcastBlock, _linkOptions, m => m != null);

            _intervalBlock.LinkTo(_intervalBlock, _linkOptions, message => !_hasCompleted && message != null && !_shouldStopIntervalBlock);
            _intervalBlock.LinkTo(_bufferRealmBlock, _linkOptions, message => !_shouldStopIntervalBlock);
            _bufferRealmBlock.LinkTo(_getNextRealmBlock, _linkOptions);
            //_bufferRealmBlock.LinkTo(_updateRealmBlock, _linkOptions, message => message.NextStep == EnumNextStep.UpdateMessage || message.NextStep == EnumNextStep.UpdateMessageInterval);

            _processCountBlock.LinkTo(DataflowBlock.NullTarget<Message>(), _linkOptions, m => m == null);
            _processCountBlock.LinkTo(_bufferRealmBlock, _linkOptions, m => m != null);

            _bufferCountBlock.LinkTo(_processCountBlock, _linkOptions);

            _handleMessageBlock.LinkTo(_bufferCountBlock, _linkOptions);

            _broadcastBlock.LinkTo(_bufferCountBlock, _linkOptions);
            _broadcastBlock.LinkTo(_handleMessageBlock, _linkOptions);
        }

        public async Task<bool> StartAsync()
        {
            var sub = await _client.GetSubscription(_subscriptionName);

            var nextMessage = new Message
            {
                RealmId = GetLeastRecentActiveRealmId()
            };

            _realmQueue.Add(new RealmInfo
            {
                RealmId = nextMessage.RealmId,
                Count = sub.UndeliveredMessageCount
            });

            var loopCount = Math.Min(_maxParallelCount, sub.UndeliveredMessageCount);

            for (int i = 0; i < loopCount; i++)
            {
                _broadcastBlock.Post(nextMessage);
            }

            _intervalBlock.Post(nextMessage);

            await _broadcastBlock.Completion;

            return true;
        }

        private string GetLeastRecentActiveRealmId()
        {
            return "shared";
        }
    }

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Message, Message>()
                .ForMember(m => m.NextStep, o => o.Ignore());
        }
    }
}
