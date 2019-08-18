namespace ĐemoMultiThread.WorkerPool
{
    public interface ISubscriptionInfo
    {
        string Subscription { get; set; }
        string RealmId { get; set; }
        int MessageCount { get; set; }
    }

    public class SubscriptionInfo : ISubscriptionInfo
    {
        public string Subscription { get; set; }
        public string RealmId { get; set; }
        public int MessageCount { get; set; }
    }

    public interface IMessage
    {
        string RealmId { get; set; }
        EnumNextStep? NextStep { get; set; }
        bool HasCompleted { get; set; }
    }

    public class Message : IMessage
    {
        public string RealmId { get; set; }
        public EnumNextStep? NextStep { get; set; }
        public bool HasCompleted { get; set; }
    }
}