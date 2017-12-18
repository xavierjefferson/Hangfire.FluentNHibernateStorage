namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _DistributedLock
    {
        public _DistributedLock()
        {
            CreatedAt = 0;
        }

        public virtual string Resource { get; set; }
        public virtual long CreatedAt { get; set; }
        public virtual string Id { get; set; }
    }
}