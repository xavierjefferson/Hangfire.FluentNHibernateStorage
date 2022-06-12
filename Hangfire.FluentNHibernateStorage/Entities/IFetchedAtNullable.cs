using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public interface IFetchedAtNullable
    {
        DateTime? FetchedAt { get; set; }
    }
}