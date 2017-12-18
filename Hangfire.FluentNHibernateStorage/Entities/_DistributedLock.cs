namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _DistributedLock:IInt32Id
    {
        public _DistributedLock()
        {
            CreatedAt = 0;
        }

        public virtual string Resource { get; set; }
        public virtual long CreatedAt { get; set; }
        public virtual int Id { get; set; }
    }
}