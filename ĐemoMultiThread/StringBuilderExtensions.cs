using System;
using System.Text;

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
            builder.AppendWithSeparator("Start " + DateTime.Now.ToString("mm:ss:fff"));
            return builder;
        }

        public static StringBuilder WriteLine(this StringBuilder builder)
        {
            Console.WriteLine(builder.ToString());
            return builder;
        }
    }
}