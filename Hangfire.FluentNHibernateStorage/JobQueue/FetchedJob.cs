namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FetchedJob
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string Queue { get; set; }
    }
}