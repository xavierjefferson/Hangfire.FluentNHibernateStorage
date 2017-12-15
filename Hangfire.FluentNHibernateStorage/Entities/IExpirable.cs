using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal interface IKeyStringValue
    {
        string Key { get; }
        string Value { get; }
    }
    internal interface IExpirable
    {
        DateTime? ExpireAt { get; set; }
    }
}