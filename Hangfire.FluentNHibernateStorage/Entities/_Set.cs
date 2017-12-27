namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _Set : KeyValueTypeBase<string>, IKeyWithStringValue
    {
        public virtual double Score { get; set; }
    }
}