using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal interface IExpirable
    {
        DateTime? ExpireAt { get; set; }
    }
}