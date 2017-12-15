using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLock : IDisposable, IComparable
    {
        private const int DelayBetweenPasses = 100;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly string DeleteDistributedLockSql = string.Format("delete from `{0}` where `{1}`=:{2}",
            nameof(_DistributedLock),
            nameof(_DistributedLock.Resource), Helper.IdParameterName);

        private readonly CancellationToken _cancellationToken;


        private readonly DateTime _start;

        private readonly TimeSpan _timeout;

        public FluentNHibernateDistributedLock(FluentNHibernateStorage storage, string resource, TimeSpan timeout,
            CancellationToken? cancellationToken = null)
        {
            Logger.TraceFormat("{2} resource={0}, timeout={1}", resource, timeout, GetType().Name);

            Resource = resource;
            _timeout = timeout;

            _cancellationToken = cancellationToken ?? new CancellationToken();
            _start = DateTime.UtcNow;
            Session = storage.GetStatefulSession();
        }


        internal IWrappedSession Session { get; private set; }

        public string Resource { get; }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var mySqlDistributedLock = obj as FluentNHibernateDistributedLock;
            if (mySqlDistributedLock != null)
                return string.Compare(Resource, mySqlDistributedLock.Resource,
                    StringComparison.InvariantCultureIgnoreCase);

            throw new ArgumentException(string.Format("Object is not a {0}", GetType().Name));
        }

        public void Dispose()
        {
            if (Session != null)
            {
                Release();
                Session.Flush();
                Session.Dispose();
                Session = null;
            }
        }

        private bool AcquireLock(string resource, TimeSpan timeout)
        {
            using (var transaction = Session.BeginTransaction())
            {
                if (!Session.Query<_DistributedLock>().Any(i =>
                    i.Resource == resource && i.CreatedAt > DateTime.UtcNow.Add(timeout.Negate())))
                {
                    Session.Insert(new _DistributedLock {CreatedAt = DateTime.UtcNow, Resource = resource});
                    Session.Flush();
                    transaction.Commit();
                    return true;
                }
                return false;
            }
        }

        internal FluentNHibernateDistributedLock Acquire()
        {
            Logger.TraceFormat("Acquire resource={0}, timeout={1}", Resource, _timeout);

            bool acquiredLock;
            do
            {
                _cancellationToken.ThrowIfCancellationRequested();

                acquiredLock = AcquireLock(Resource, _timeout);

                if (ContinueCondition(acquiredLock))
                {
                    _cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                    _cancellationToken.ThrowIfCancellationRequested();
                }
            } while (ContinueCondition(acquiredLock));

            if (!acquiredLock )
            {
                throw new FluentNHibernateDistributedLockException("cannot acquire lock");
            }
            return this;
        }

        private bool ContinueCondition(bool acquiredLock)
        {
            return !acquiredLock  && _start.Add(_timeout) > DateTime.UtcNow;
        }

        internal void Release()
        {
            Logger.TraceFormat("Release resource={0}", Resource);

            Session.CreateQuery(DeleteDistributedLockSql).SetParameter(Helper.IdParameterName, Resource).ExecuteUpdate();
        }
    }
}