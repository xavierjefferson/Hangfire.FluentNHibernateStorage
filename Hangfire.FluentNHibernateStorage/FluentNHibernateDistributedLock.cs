using System;
using System.Data;
using System.Threading;
using Hangfire.Logging;
using Newtonsoft.Json;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLock : IDisposable, IComparable
    {
        private const int DelayBetweenPasses = 100;
        private static readonly object Mutex = new object();
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly CancellationToken _cancellationToken;
        private readonly string _resource;
        private readonly FluentNHibernateJobStorage _storage;
        private readonly TimeSpan _timeout;
        private bool _acquired;

        public FluentNHibernateDistributedLock(FluentNHibernateJobStorage storage, string resource, TimeSpan timeout,
            CancellationToken? cancellationToken = null)
        {
            Logger.TraceFormat("{2} resource={0}, timeout={1}", resource, timeout, GetType().Name);

            _resource = resource;
            _timeout = timeout;
            _cancellationToken = cancellationToken ?? new CancellationToken();
            _storage = storage;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var distributedLock = obj as FluentNHibernateDistributedLock;
            if (distributedLock != null)
                return string.Compare(_resource, distributedLock._resource,
                    StringComparison.InvariantCultureIgnoreCase);

            throw new ArgumentException(string.Format("Object is not a {0}", GetType().Name));
        }

        public void Dispose()
        {
            if (_acquired)
                Release();
        }

        internal FluentNHibernateDistributedLock Acquire()
        {
            var finish = DateTime.Now.Add(_timeout);

            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();


                if (SqlUtil.WrapForTransaction(CreateLockRow))
                {

                    Logger.DebugFormat("Granted distributed lock for {0}", _resource);
                    _acquired = true;
                    return this;
                }


                if (finish > DateTime.Now)
                    _cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                else
                {
                    throw new FluentNHibernateDistributedLockException("cannot acquire lock");
                }
            }
        }

        private bool CreateLockRow()
        {
            lock (Mutex)
            {
                using (var session = _storage.GetSession())
                {
                    using (var transaction = session.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var lockResourceParams = new LockResourceParams(session, _resource, _timeout);

                        var query = session.CreateQuery(SqlUtil.GetCreateDistributedLockStatement())
                            .SetParameter(SqlUtil.ResourceParameterName, _resource)
                            .SetParameter(SqlUtil.ExpireAtAsLongParameterName,
                                lockResourceParams.expireAtAsLong)
                            .SetParameter(SqlUtil.NowParameterName, lockResourceParams.utcNow)
                            .SetParameter(SqlUtil.NowAsLongParameterName, lockResourceParams.nowAsLong);


                        var count = query.ExecuteUpdate();
                        if (count == 1)
                        {
                            transaction.Commit();
                            Logger.DebugFormat("Created distributed lock, count={0} for {1}", count,
                                JsonConvert.SerializeObject(lockResourceParams));
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal void Release()
        {
            SqlUtil.WrapForTransaction(() =>
            {
                lock (Mutex)
                {
                    using (var session = _storage.GetSession())
                    {
                        session.CreateQuery(SqlUtil.DeleteDistributedLockSql)
                            .SetParameter(SqlUtil.IdParameterName, _resource)
                            .ExecuteUpdate();
                        session.Flush();
                        Logger.DebugFormat("Released distributed lock for {0}", _resource);
                    }
                }
            });
        }

        private class LockResourceParams
        {
            public LockResourceParams(SessionWrapper session, string resource, TimeSpan timeout)
            {
                Resource = resource;
                utcNow = session.Storage.UtcNow;
                _timeout = timeout;
            }

            public string Resource { get; }
            public DateTime utcNow { get; }
            public TimeSpan _timeout { get; }
            public long expireAtAsLong => utcNow.Add(_timeout).ToUnixDate();

            public long nowAsLong => utcNow.ToUnixDate();
        }
    }
}