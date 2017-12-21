using System;
using System.Data;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using NHibernate;
using NHibernate.Exceptions;

namespace Hangfire.FluentNHibernateStorage
{
    public abstract class FluentNHibernateDistributedLockBase : IDisposable, IComparable
    {
        private const int DelayBetweenPasses = 100;
        protected static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private bool _acquired;

        protected CancellationToken CancellationToken;

        protected TimeSpan Timeout;

        protected FluentNHibernateDistributedLockBase(FluentNHibernateJobStorage storage, string resource,
            TimeSpan timeout,
            CancellationToken? cancellationToken = null)
        {
            Logger.TraceFormat("{2} resource={0}, timeout={1}", resource, timeout, GetType().Name);

            Resource = resource;
            Timeout = timeout;
            CancellationToken = cancellationToken ?? new CancellationToken();
            Session = storage.GetStatefulSession();
        }


        internal IWrappedSession Session { get; set; }
        public string Resource { get; protected set; }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var mySqlDistributedLock = obj as FluentNHibernateDistributedLockBase;
            if (mySqlDistributedLock != null)
                return string.Compare(Resource, mySqlDistributedLock.Resource,
                    StringComparison.InvariantCultureIgnoreCase);

            throw new ArgumentException(string.Format("Object is not a {0}", GetType().Name));
        }

        public void Dispose()
        {
            if (Session == null)
            {
                return;
            }
            if (_acquired)
            {
                Release();
            }
            Session.Flush();
            Session.Dispose();
            Session = null;
        }

        internal FluentNHibernateDistributedLockBase Acquire()
        {
            var start = DateTime.UtcNow;
            var finish = start.Add(Timeout);
            Session.Flush();
            while (true)
            {
                CancellationToken.ThrowIfCancellationRequested();
                var now = DateTime.UtcNow.ToUnixDate();
                var expired = DateTime.UtcNow.Subtract(Timeout).ToUnixDate();

                if (SQLHelper.WrapForTransaction(() =>
                {
                    using (var transaction = Session.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var stmt =
                            string.Format(@"insert into {0} ({2}, {1}, {3})
                        select :resource, :createdAt, :expires from {4} where not exists(select Id from 
                        {0} as d where d.{2} = :resource and d.{1} < :createdAt)",
                        nameof(_DistributedLock), nameof(_DistributedLock.CreatedAt), nameof(_DistributedLock.Resource), nameof(_DistributedLock.ExpireAt), nameof(_Dual));
                        var p = Session.CreateQuery(stmt).SetParameter("resource", Resource)
                            .SetParameter("createdAt", expired)
                            .SetParameter("expires", DateTime.UtcNow.AddDays(5));
                        if (p.ExecuteUpdate() > 0)
                        {
                            transaction.Commit();
                            return true;
                        }

                        //if (!Session.Query<_DistributedLock>()
                        //    .Any(i => i.Resource == Resource && i.CreatedAt > expired))
                        //{
                        //    Session.Insert(new _DistributedLock
                        //    {
                        //        CreatedAt = now,
                        //        Resource = Resource
                        //    });
                        //    Session.Flush();
                        
                        //    return true;
                        //}
                    }
                    return false;
                }))
                {
                    Logger.DebugFormat("Granted distributed lock for {0}", Resource);
                    _acquired = true;
                    return this;
                }

                if (finish > DateTime.UtcNow)
                {
                    CancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                }
                else
                {
                    throw new FluentNHibernateDistributedLockException("cannot acquire lock");
                }
            }
        }

        internal void Release()
        {
            SQLHelper.WrapForTransaction(() =>
            {
                Session.CreateQuery(SQLHelper.DeleteDistributedLockSql)
                    .SetParameter(SQLHelper.IdParameterName, Resource)
                    .ExecuteUpdate();
                Logger.DebugFormat("Released distributed lock for {0}", Resource);
            });
        }
    }
}