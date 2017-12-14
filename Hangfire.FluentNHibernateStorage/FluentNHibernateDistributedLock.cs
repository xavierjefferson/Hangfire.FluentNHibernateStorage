using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLock : IDisposable, IComparable
    {
        private const int DelayBetweenPasses = 100;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly string DeleteDistributedLockSql = string.Format("delete from {0} where {1}=:res",
            nameof(_DistributedLock),
            nameof(_DistributedLock.Resource));

        private readonly CancellationToken _cancellationToken;

        private readonly ISession _session;

        private readonly DateTime _start;
        private readonly FluentNHibernateStorage _storage;
        private readonly TimeSpan _timeout;

        public FluentNHibernateDistributedLock(FluentNHibernateStorage storage, string resource, TimeSpan timeout)
            : this(storage.GetStatefulSession(), resource, timeout)
        {
            _storage = storage;
        }

        public FluentNHibernateDistributedLock(ISession session, string resource, TimeSpan timeout)
            : this(session, resource, timeout, new CancellationToken())
        {
        }

        public FluentNHibernateDistributedLock(
            ISession session, string resource, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Logger.TraceFormat("{2} resource={0}, timeout={1}", resource, timeout, this.GetType().Name);

            Resource = resource;
            _timeout = timeout;
            _session = session;
            _cancellationToken = cancellationToken;
            _start = DateTime.UtcNow;
        }

        public string Resource { get; }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var mySqlDistributedLock = obj as FluentNHibernateDistributedLock;
            if (mySqlDistributedLock != null)
                return string.Compare(Resource, mySqlDistributedLock.Resource,
                    StringComparison.InvariantCultureIgnoreCase);

            throw new ArgumentException(string.Format("Object is not a {0}", this.GetType().Name));
        }

        public void Dispose()
        {
            Release();

            _storage?.ReleaseSession(_session);
        }

        private int AcquireLock(string resource, TimeSpan timeout)
        {
            if (!_session.Query<_DistributedLock>().Any(i =>
                i.Resource == resource && i.CreatedAt > DateTime.UtcNow.Add(timeout.Negate())))
            {
                _session.Save(new _DistributedLock {CreatedAt = DateTime.UtcNow, Resource = resource});
                _session.Flush();
                return 1;
            }
            return 0;
        }

        internal FluentNHibernateDistributedLock Acquire()
        {
            Logger.TraceFormat("Acquire resource={0}, timeout={1}", Resource, _timeout);

            int insertedObjectCount;
            do
            {
                _cancellationToken.ThrowIfCancellationRequested();

                insertedObjectCount = AcquireLock(Resource, _timeout);

                if (ContinueCondition(insertedObjectCount))
                {
                    _cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                    _cancellationToken.ThrowIfCancellationRequested();
                }
            } while (ContinueCondition(insertedObjectCount));

            if (insertedObjectCount == 0)
            {
                throw new FluentNHibernateDistributedLockException("cannot acquire lock");
            }
            return this;
        }

        private bool ContinueCondition(int insertedObjectCount)
        {
            return insertedObjectCount == 0 && _start.Add(_timeout) > DateTime.UtcNow;
        }

        internal void Release()
        {
            Logger.TraceFormat("Release resource={0}", Resource);

            _session.CreateQuery(DeleteDistributedLockSql).SetParameter("res", Resource).ExecuteUpdate();
        }
    }
}