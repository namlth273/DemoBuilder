using AutoFixture;
using System;
using System.Threading.Tasks;

namespace ĐemoMultiThread
{
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
                SubscriptionType = Guid.NewGuid().ToString(),
                UndeliveredMessageCount = 18
            });
        }

        public Task<IMessageBody> PullMessage(string subscription)
        {
            return Task.FromResult(_fixture.Create<MessageBody>() as IMessageBody);
        }
    }
}