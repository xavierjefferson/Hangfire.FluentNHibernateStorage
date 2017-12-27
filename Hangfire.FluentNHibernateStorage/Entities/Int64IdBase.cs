namespace Hangfire.FluentNHibernateStorage.Entities
{
    public abstract class Int64IdBase : IInt64Id
    {
        public virtual long Id { get; set; }
    }
}