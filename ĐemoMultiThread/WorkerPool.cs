using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ĐemoMultiThread
{
    public class WorkerPool<T> where T : IMessage
    {
        private readonly Func<T, Task> _handleMessage;
        private readonly List<T> _queue = new List<T>();
        private TransformBlock<T, T> _firstBlock;
        private BufferBlock<T> _bufferBlock;
        private ActionBlock<T> _lastBlock;
        private readonly ExecutionDataflowBlockOptions _executionBlockOptions;
        private readonly DataflowLinkOptions _linkOptions;
        private int _proceededMessageCount = 0;

        private BroadcastBlock<T> _broadcastBlock;
        private BufferBlock<T> _bufferCountBlock;
        private TransformBlock<T, T> _handleMessageBlock;
        private TransformBlock<T, T> _processCountBlock;
        private BufferBlock<T> _bufferSubscriptionBlock;
        private TransformBlock<T, T> _getNextSubscriptionBlock;
        private TransformBlock<T, T> _updateSubscriptionBlock;
        private BroadcastBlock<T> _intervalBlock;

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
            _broadcastBlock = new BroadcastBlock<T>(message => message);
            _bufferCountBlock = new BufferBlock<T>(_executionBlockOptions);
            _handleMessageBlock = new TransformBlock<T, T>(async message =>
            {
                await _handleMessage(message);

                return message;
            });
            _processCountBlock = new TransformBlock<T, T>(async message =>
            {
                //TODO implement process count

                return message;
            });
            _bufferSubscriptionBlock = new BufferBlock<T>(_executionBlockOptions);
            _getNextSubscriptionBlock = new TransformBlock<T, T>(message =>
            {
                //TODO implement get next subscription

                return message;
            });
            _updateSubscriptionBlock = new TransformBlock<T, T>(message =>
            {
                //TODO implement update subscription

                return message;
            });
            _intervalBlock = new BroadcastBlock<T>(message =>
            {
                //TODO delay 30s
                //TODO implement interval to get subscription from API

                return message;
            });

            _intervalBlock.LinkTo(_intervalBlock);
            _intervalBlock.LinkTo(_bufferSubscriptionBlock);
            _bufferSubscriptionBlock.LinkTo(_getNextSubscriptionBlock, _linkOptions, message => message.IsGetNextSubscription);
            _bufferSubscriptionBlock.LinkTo(_updateSubscriptionBlock, _linkOptions, message => message.IsUpdateSubscription);
            _processCountBlock.LinkTo(_bufferSubscriptionBlock, _linkOptions);
            _bufferBlock.LinkTo(_processCountBlock, _linkOptions);
            _handleMessageBlock.LinkTo(_bufferBlock, _linkOptions);
            _broadcastBlock.LinkTo(_bufferCountBlock, _linkOptions);
            _broadcastBlock.LinkTo(_handleMessageBlock, _linkOptions);







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

        public async Task<bool> StartAsync()
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
}
