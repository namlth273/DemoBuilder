namespace ĐemoMultiThread
{
    public interface IMessage
    {
        string Subscription { get; set; }
        string RealmId { get; set; }
        bool IsUpdateSubscription { get; set; }
        bool IsGetNextSubscription { get; set; }
        bool HasNextSubscription { get; set; }
    }

    public class Message : IMessage
    {
        public string Subscription { get; set; }
        public string RealmId { get; set; }
        public bool IsUpdateSubscription { get; set; }
        public bool IsGetNextSubscription { get; set; }
        public bool HasNextSubscription { get; set; }
    }

    public interface IMessageBody
    {
        string FirstName { get; set; }
        string LastName { get; set; }
    }

    public class MessageBody : IMessageBody
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public interface ISubscriptionInfoV2
    {
        string SubscriptionType { get; set; }
        bool IsPaused { get; set; }
        int UndeliveredMessageCount { get; set; }
    }

    public class SubscriptionInfoV2 : ISubscriptionInfoV2
    {
        public string SubscriptionType { get; set; }
        public bool IsPaused { get; set; }
        public int UndeliveredMessageCount { get; set; }
    }
}
