using System;
using System.Text;
using System.Threading.Tasks;

namespace ĐemoMultiThread.WorkerPool
{
    public interface IDataFlowHandler
    {
        Task HandleAsync(ISubscriptionInfo subscription);
    }

    public class ProcessMessageBlock : IDataFlowHandler
    {
        public async Task HandleAsync(ISubscriptionInfo subscription)
        {
            var builder = new StringBuilder()
                .AppendStartDate()
                .AppendWithSeparator("Process".PadRight(11));
            //.AppendWithSeparator($"Id {request.Id.ToString().Substring(0, 3)}")
            //.AppendWithSeparator($"MessageCount {request.MessageCount:00}")
            //.AppendWithSeparator($"Type {request.RequestType.PadRight(22)}");

            var random = new Random().Next(5, 18);

            await Task.Delay(TimeSpan.FromSeconds(1));

            //builder
            //    .AppendWithSeparator($"Delay {random}")
            //    .AppendEndDate()
            //    .WriteLine();

            //request.LastProcessedDate = DateTime.Now;
            //request.MessageCount--;
        }
    }
}
