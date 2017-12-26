namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FetchedJob
    {
        public long Id { get; set; }
        public long JobId { get; set; }
        public string Queue { get; set; }
    }
}