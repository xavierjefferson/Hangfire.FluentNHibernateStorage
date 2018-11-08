namespace Hangfire.FluentNHibernateStorage.Entities
{
    public abstract class Int32IdBase : IInt32Id
    {
        public virtual int Id { get; set; }
    }
}