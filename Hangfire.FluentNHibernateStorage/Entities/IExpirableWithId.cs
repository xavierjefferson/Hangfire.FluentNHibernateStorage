namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal interface IExpirableWithId : IExpirable, IInt64Id
    {
    }
}