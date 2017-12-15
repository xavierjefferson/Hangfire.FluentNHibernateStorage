namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal interface IKeyWithStringValue
    {
        string Key { get; }
        string Value { get; }
    }
}