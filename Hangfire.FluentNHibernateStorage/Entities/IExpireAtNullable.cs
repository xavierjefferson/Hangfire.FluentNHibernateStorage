using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public interface IExpireAtNullable
    {
        DateTime? ExpireAt { get; set; }
    }

    public interface IFetchedAtNullable
    {
        DateTime? FetchedAt { get; set; }
    }
}