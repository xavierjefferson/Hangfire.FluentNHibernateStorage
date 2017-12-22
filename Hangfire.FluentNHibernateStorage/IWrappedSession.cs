using System;
using System.Data;
using System.Linq;
using NHibernate;

namespace Hangfire.FluentNHibernateStorage
{
    public interface IWrappedSession : IDisposable
    {
        void Truncate<T>();
        ProviderTypeEnum ProviderType { get; }
        FluentNHibernateJobStorage Storage { get; }
        ITransaction BeginTransaction(IsolationLevel level);
        ITransaction BeginTransaction();
        IQueryable<T> Query<T>();
        IQuery CreateQuery(string queryString);
        int ExecuteQuery(string queryString);
        ISQLQuery CreateSqlQuery(string queryString);
        void Insert(object x);
        void Update(object x);
        void Flush();
    }
}