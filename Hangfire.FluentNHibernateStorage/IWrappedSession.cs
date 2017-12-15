using System;
using System.Linq;
using NHibernate;

namespace Hangfire.FluentNHibernateStorage
{
    internal interface IWrappedSession : IDisposable
    {
        ITransaction BeginTransaction();

        IQueryable<T> Query<T>();
        IQuery CreateQuery(string queryString);
        void Insert(object x);
        void Update(object x);
        void Flush();
    }
}