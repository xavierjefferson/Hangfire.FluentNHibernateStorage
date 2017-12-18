using System;
using System.Data;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Maps;
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

        private bool Acquire(long expired, long now)
        {
            var resource = Resource;
            try
            {
                using (var transaction = Session.BeginTransaction(IsolationLevel.Serializable))
                {
                    if (!Session.Query<_DistributedLock>().Any(i =>
                        i.Resource == resource && i.CreatedAt > expired))
                    {
                        Session.Insert(new _DistributedLock
                        {
                            CreatedAt = now,
                            Resource = resource 
                            
                        });
                        Session.Flush();
                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (TransactionException)
            {
                //do nothing
            }
            catch (GenericADOException)
            {
                //do nothing
            }

            return false;
        }

        internal FluentNHibernateDistributedLockBase Acquire()
        {
            Logger.TraceFormat("Acquire resource={0}, timeout={1}", Resource, Timeout);
            var start = DateTime.UtcNow;
            var finish = start.Add(Timeout);
            Session.Flush();
            while (true)
            {
                CancellationToken.ThrowIfCancellationRequested();
                var now = DateTime.UtcNow.ToUnixDate();
                var expired = DateTime.UtcNow.Subtract(Timeout).ToUnixDate();

                if (Acquire(expired, now))
                {
                    Logger.InfoFormat("Granted distributed lock for {0}", Resource);
                    _acquired = true;
                    return this;
                }

                if (finish > DateTime.UtcNow)
                {
                    CancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);

                    start = DateTime.UtcNow;
                    finish = start.Add(Timeout);
                }
                else
                {
                    throw new FluentNHibernateDistributedLockException("cannot acquire lock");
                }
            }
        }

        internal void Release()
        {
            Logger.TraceFormat("Release resource={0}", Resource);
            Logger.InfoFormat("Released distributed lock for {0}", Resource);

            Session.CreateQuery(SQLHelper.DeleteDistributedLockSql).SetParameter(SQLHelper.IdParameterName, Resource)
                .ExecuteUpdate();
        }
    }
}