namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal interface IExpireWithId : IExpirable
    {
        int Id { get; }
    }
}