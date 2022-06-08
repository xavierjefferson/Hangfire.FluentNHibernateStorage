using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public interface ICreatedAt
    {
        DateTime CreatedAt { get; set; }
    }
}