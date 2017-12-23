using System;
using System.Data;
using System.Linq;
using NHibernate;

namespace Hangfire.FluentNHibernateStorage
{
    public interface IWrappedSessionBase
    {
        ProviderTypeEnum ProviderType { get; }
        FluentNHibernateJobStorage Storage { get; }
        void DeleteAll<T>();
        int ExecuteQuery(string queryString);
    }

    public interface IWrappedSession : IDisposable, IWrappedSessionBase
    {
        void Clear();
        ITransaction BeginTransaction(IsolationLevel level);
        ITransaction BeginTransaction();
        IQueryable<T> Query<T>();
        IQuery CreateQuery(string queryString);
        void Evict(object x);
        ISQLQuery CreateSqlQuery(string queryString);
        void Insert(object x);
        void Update(object x);
        void Flush();
    }
}