using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public interface IExpirable
    {
        DateTime? ExpireAt { get; set; }
    }
}