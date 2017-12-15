namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal interface IExpirableWithKey : IExpirable
    {
        string Key { get; set; }
    }
}