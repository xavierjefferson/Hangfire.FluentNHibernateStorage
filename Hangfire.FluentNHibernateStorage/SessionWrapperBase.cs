﻿using NHibernate;

namespace Hangfire.FluentNHibernateStorage
{
    public abstract class SessionWrapperBase:IWrappedSessionBase
    {
        public FluentNHibernateJobStorage Storage { get; protected set; }
        public ProviderTypeEnum ProviderType { get; protected set; }
        public abstract IQuery CreateQuery(string query);

        public void DeleteAll<T>()
        {
            ExecuteQuery(string.Format("delete from {0}", typeof(T).Name));
        }
        public int ExecuteQuery(string queryString)
        {
            return CreateQuery(queryString).ExecuteUpdate();
        }
    }
}