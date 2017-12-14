using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal interface IExpireWithKey
    {
        string Key { get; set; }
        DateTime? ExpireAt { get; set; }
    }
}