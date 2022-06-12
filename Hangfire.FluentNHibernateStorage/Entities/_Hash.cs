namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _Hash : KeyValueTypeBase<string>, IStringKey, IStringValue
    {
        public virtual string Field { get; set; }
    }
}