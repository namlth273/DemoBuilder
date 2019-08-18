using ĐemoMultiThread.WorkerPool;
using System;
using System.Text;
using System.Threading;

namespace ĐemoMultiThread
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendWithSeparator(this StringBuilder builder, string value)
        {
            builder.Append(value).Append(" | ");
            return builder;
        }

        public static StringBuilder AppendCount(this StringBuilder builder, TaskPool taskPool)
        {
            builder.AppendWithSeparator("Queue count " + taskPool.DefaultQueue.Count)
                .AppendWithSeparator("WorkingTasks count " + taskPool.WorkingTasks.Count);
            return builder;
        }

        public static StringBuilder AppendStartDate(this StringBuilder builder)
        {
            builder.AppendWithSeparator("Start " + DateTime.Now.ToString("mm:ss:ffff"))
                .AppendWithSeparator($"Thread {Thread.CurrentThread.ManagedThreadId.ToString("00")}");
            return builder;
        }

        public static StringBuilder AppendEndDate(this StringBuilder builder)
        {
            builder.AppendWithSeparator("End " + DateTime.Now.ToString("mm:ss:ffff"));
            return builder;
        }

        public static StringBuilder WriteLine(this StringBuilder builder)
        {
            Console.WriteLine(builder.ToString());
            return builder;
        }

        public static StringBuilder AppendSubscription(this StringBuilder builder, string subscription)
        {
            builder.AppendWithSeparator(
                $"Sub {subscription.Substring(subscription.Length - 3, 3)}");
            return builder;
        }

        public static StringBuilder AppendNextStep(this StringBuilder builder, WorkerPool.IMessage message)
        {
            builder.AppendWithSeparator($"Next? {message.NextStep == EnumNextStep.GetNextMessage}")
                .AppendWithSeparator($"Update? {message.NextStep == EnumNextStep.UpdateMessage}");
            return builder;
        }
    }
}