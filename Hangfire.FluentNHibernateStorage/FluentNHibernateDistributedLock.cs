using System;
using System.Data;
using System.Threading;
using Hangfire.Logging;
using Newtonsoft.Json;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLock : IDisposable, IComparable
    {
        private static readonly object Mutex = new object();
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly CancellationToken _cancellationToken;
        private readonly FluentNHibernateStorageOptions _options;
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
            _options = storage.Options;
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
            var finish = DateTime.UtcNow.Add(_timeout);

            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();


                if (SqlUtil.WrapForTransaction(CreateLockRow))
                {
                    Logger.DebugFormat("Granted distributed lock for {0}", _resource);
                    _acquired = true;
                    return this;
                }


                if (finish > DateTime.UtcNow)
                    _cancellationToken.WaitHandle.WaitOne(_options.DistributedLockPollInterval);
                else
                    throw new FluentNHibernateDistributedLockException("cannot acquire lock");
            }
        }

        private bool CreateLockRow()
        {
            return SqlUtil.WrapForDeadlock(() =>
            {
                lock (Mutex)
                {
                    using (var session = _storage.GetSession())
                    {
                        using (var transaction = session.BeginTransaction(IsolationLevel.Serializable))
                        {
                            var lockResourceParams = new LockResourceParams(session, _resource, _timeout);

                            var query = session.CreateQuery(SqlUtil.GetCreateDistributedLockStatement())
                                .SetParameter(SqlUtil.ResourceParameterName, lockResourceParams.Resource)
                                .SetParameter(SqlUtil.ExpireAtAsLongParameterName,
                                    lockResourceParams.ExpireAtAsLong)
                                .SetParameter(SqlUtil.CreatedAtParameterName, lockResourceParams.CreatedAt)
                                .SetParameter(SqlUtil.UtcNowAsLongParameterName, lockResourceParams.CreatedAtAsLong);


                            var count = query.ExecuteUpdate();
                            if (count == 1)
                            {
                                transaction.Commit();
                                if (Logger.IsDebugEnabled())
                                    Logger.DebugFormat("Created distributed lock for {0}",
                                        JsonConvert.SerializeObject(lockResourceParams));
                                return true;
                            }
                        }
                    }

                    return false;
                }
            }, _options);
        }

        internal void Release()
        {
            SqlUtil.WrapForTransaction(() =>
            {
                SqlUtil.WrapForDeadlock(() =>
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
                }, _options);
            });
        }

        private class LockResourceParams
        {
            public LockResourceParams(SessionWrapper session, string resource, TimeSpan timeout)
            {
                Resource = resource;
                CreatedAt = session.Storage.UtcNow;
                ExpireAtAsLong = CreatedAt.Add(timeout).ToUnixDate();
                CreatedAtAsLong = CreatedAt.ToUnixDate();
            }

            public string Resource { get; }
            public DateTime CreatedAt { get; }
            public long ExpireAtAsLong { get; }
            public long CreatedAtAsLong { get; }
        }
    }
}