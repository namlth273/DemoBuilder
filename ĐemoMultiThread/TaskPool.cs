using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ĐemoMultiThread
{
    public class TaskPool
    {
        private readonly object _tasksMutex = new object();
        private readonly object _checkMutex = new object();
        private readonly int _threadsMaxCount;

        public HashSet<IInternalTask> WorkingTasks { get; set; }
        public ConcurrentQueue<IInternalTask> DefaultQueue { get; set; }

        public interface IInternalTask
        {
            Task Execute();
        }

        private class InternalTaskHolder : IInternalTask
        {
            public Func<Task> Task { get; set; }
            public TaskCompletionSource<IDisposable> Waiter { get; set; }

            public async Task Execute()
            {
                await Task();
                Waiter.SetResult(null);
            }
        }

        private class InternalTaskHolderGeneric<T> : IInternalTask
        {
            public Func<Task<T>> Task { get; set; }
            public TaskCompletionSource<T> Waiter { get; set; }

            public async Task Execute()
            {
                var result = await Task();
                Waiter.SetResult(result);
            }
        }

        /// <summary>
        /// Raised when all tasks have been completed.
        /// </summary>
        public event EventHandler Completed;

        /// <summary>
        /// Creates a new thread queue with a maximum number of threads
        /// </summary>
        /// <param name="threadsMaxCount">The maximum number of currently threads</param>
        public TaskPool(int threadsMaxCount)
        {
            WorkingTasks = new HashSet<IInternalTask>();
            DefaultQueue = new ConcurrentQueue<IInternalTask>();
            _threadsMaxCount = threadsMaxCount;
        }

        /// <summary>
        /// Creates a new thread queue with a maximum number of threads and the tasks that should be executed.
        /// </summary>
        /// <param name="threadsMaxCount">The maximum number of currently threads.</param>
        /// <param name="tasks">The tasks that will be execute in pool.</param>
        public TaskPool(int threadsMaxCount, IList<Func<Task>> tasks) : this(threadsMaxCount)
        {
            foreach (var task in tasks)
            {
                DefaultQueue.Enqueue(new InternalTaskHolder { Task = task, Waiter = new TaskCompletionSource<IDisposable>() });
            }
        }

        /// <summary>
        /// Adds a task and runs it if free thread exists. Otherwise enqueue.
        /// </summary>
        /// <param name="task">The task that will be execute</param>
        /// <param name="waitUntilStart">Task will be enqueue and not running yet.</param>
        public Task EnqueueAsync(Func<Task> task, bool waitUntilStart = false)
        {
            lock (_tasksMutex)
            {
                var holder = new InternalTaskHolder { Task = task, Waiter = new TaskCompletionSource<IDisposable>() };

                if (waitUntilStart || WorkingTasks.Count >= _threadsMaxCount)
                    DefaultQueue.Enqueue(holder);
                else
                    StartTask(holder);

                return Task.CompletedTask;
                //return holder.Waiter.Task;
            }
        }

        /// <summary>
        /// Adds a task and runs it if free thread exists. Otherwise enqueue.
        /// </summary>
        /// <param name="task">The task that will be execute</param>
        public Task<T> EnqueueAsync<T>(Func<Task<T>> task)
        {
            lock (_tasksMutex)
            {
                var holder = new InternalTaskHolderGeneric<T> { Task = task, Waiter = new TaskCompletionSource<T>() };

                if (WorkingTasks.Count >= _threadsMaxCount)
                    DefaultQueue.Enqueue(holder);
                else
                    StartTask(holder);

                return holder.Waiter.Task;
            }
        }

        public async Task StartTaskAsync()
        {
            await CheckQueueAsync();
        }

        /// <summary>
        /// Starts the execution of a task.
        /// </summary>
        /// <param name="task">The task that should be executed.</param>
        private async void StartTask(IInternalTask task)
        {
            WorkingTasks.Add(task);
            await task.Execute();
            await TaskCompletedAsync(task);
        }

        private Task TaskCompletedAsync(IInternalTask task)
        {
            lock (_tasksMutex)
            {
                WorkingTasks.Remove(task);

                CheckQueueAsync();

                if (DefaultQueue.Count == 0 && WorkingTasks.Count == 0)
                    OnCompleted();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if the queue contains tasks and runs as many as there are free execution slots.
        /// </summary>
        private Task CheckQueueAsync()
        {
            lock (_checkMutex)
                while (DefaultQueue.Count > 0 && WorkingTasks.Count < _threadsMaxCount)
                    if (DefaultQueue.TryDequeue(out IInternalTask task))
                        StartTask(task);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Raises the Completed event.
        /// </summary>
        protected void OnCompleted()
        {
            Completed?.Invoke(this, null);
        }
    }
}