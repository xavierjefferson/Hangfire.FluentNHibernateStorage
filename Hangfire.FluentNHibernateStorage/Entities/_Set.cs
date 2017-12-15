namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal class _Set : EntityBase1<string>, IExpirableWithKey, IExpirableWithId, IKeyWithStringValue
    {
        public virtual double Score { get; set; }
    }
}