namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _Set : KeyValueTypeBase<string>, IKeyWithStringValue, IStringValue
    {
        public virtual double Score { get; set; }
    }
}