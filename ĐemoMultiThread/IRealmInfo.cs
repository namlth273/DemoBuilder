namespace ĐemoMultiThread
{
    public interface IRealmInfo
    {
        string RealmId { get; set; }
        int Count { get; set; }
    }

    public class RealmInfo : IRealmInfo
    {
        public string RealmId { get; set; }
        public int Count { get; set; }
    }
}