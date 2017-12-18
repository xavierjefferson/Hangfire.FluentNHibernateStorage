using System;
using System.Data;
using System.Linq;
using NHibernate;

namespace Hangfire.FluentNHibernateStorage
{
    public interface IWrappedSession : IDisposable
    {
        ITransaction BeginTransaction(IsolationLevel iso);
        ITransaction BeginTransaction();
        IQueryable<T> Query<T>();
        IQuery CreateQuery(string queryString);
        ISQLQuery CreateSqlQuery(string queryString);
        void Insert(object x);
        void Update(object x);
        void Flush();
    }
}