using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ĐemoMultiThread
{
    class Program
    {
        static int _numCurrentThreads = 0;

        static readonly Random Rnd = new Random();

        static void Main(string[] args)
        {
            var services = new ServiceCollection();

            services.AddMemoryCache();

            var builder = new ContainerBuilder();

            builder.Populate(services);

            builder.RegisterModule<AutofacModule>();

            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var cache = scope.Resolve<IMemoryCache>();

                using (var entry = cache.CreateEntry("TaskPool"))
                {
                    entry.Value =new TaskPool(2);
                }

                var publisher = scope.Resolve<Publisher>();

                var source = new CancellationTokenSource();

                //var changeToken = new CancellationChangeToken(source.Token);

                //var compositeChangeToken =
                //    new CompositeChangeToken(
                //        new List<IChangeToken>
                //        {
                //            changeToken
                //        });

                //compositeChangeToken.RegisterChangeCallback(Console.WriteLine, source);

                //publisher.Publish(new TestMultiThread.Command
                //{
                //    TokenSource = source,
                //    Id = Guid.NewGuid()
                //}, PublishStrategy.ParallelWhenAll, source.Token);

                var innerTokenSource = new CancellationTokenSource();

                publisher.Publish(new TestWorkerPool.Command
                {
                    //TokenSource = innerTokenSource,
                    Id = Guid.NewGuid()
                }, PublishStrategy.ParallelWhenAll, source.Token);

                Console.WriteLine("Press any key to cancel tasks");
                Console.ReadLine();
                innerTokenSource.Cancel(false);
                Console.WriteLine("IsCancellationRequested " + source.IsCancellationRequested);
                Console.ReadLine();
            }

            //int maxParallelTasks = 4;
            //int totalTasks = 10;

            //using (var blockingCounter = new BlockingCounter(maxParallelTasks))
            //{
            //    for (int i = 1; i <= totalTasks; i++)
            //    {
            //        Console.WriteLine("Submitting task {0}", i);
            //        blockingCounter.WaitableIncrement();
            //        if (!ThreadPool.QueueUserWorkItem((obj) =>
            //        {
            //            try
            //            {
            //                ThreadProc(obj);
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.Error.WriteLine("Task {0} failed: {1}", obj, ex.Message);
            //            }
            //            finally
            //            {
            //                // Exceptions are possible here too, 
            //                // but proper error handling is not the goal of this sample
            //                blockingCounter.WaitableDecrement();
            //            }
            //        }, i))
            //        {
            //            blockingCounter.WaitableDecrement();
            //            Console.Error.WriteLine("Failed to submit task {0} for execution.", i);
            //        }
            //    }

            //    Console.WriteLine("Waiting for copmletion...");
            //    blockingCounter.CloseAndWait(30000);
            //}

            //Console.WriteLine("Work done!");
            //Console.ReadKey();
        }

        static void ThreadProc(object obj)
        {
            int taskNumber = (int)obj;
            int numThreads = Interlocked.Increment(ref _numCurrentThreads);

            Console.WriteLine("Task {0} started. Total: {1}", taskNumber, numThreads);
            int sleepTime = Rnd.Next(0, 5);
            Task.Delay(sleepTime * 1000);
            Console.WriteLine("Task {0} finished.", taskNumber);

            Interlocked.Decrement(ref _numCurrentThreads);
        }
    }
}