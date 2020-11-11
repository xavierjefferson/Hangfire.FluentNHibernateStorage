using System;
using System.Data;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Newtonsoft.Json;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLock : IDisposable, IComparable
    {
        private static readonly object Mutex = new object();
        private static readonly ILog Logger = LogProvider.For<FluentNHibernateDistributedLock>();
        private readonly CancellationToken _cancellationToken;
        private readonly FluentNHibernateStorageOptions _options;
        private readonly string _resource;
        private readonly FluentNHibernateJobStorage _storage;
        private readonly TimeSpan _timeout;
        private int? _lockId;

        public FluentNHibernateDistributedLock(FluentNHibernateJobStorage storage, string resource, TimeSpan timeout,
            CancellationToken? cancellationToken = null)
        {
            Logger.DebugFormat("{2} resource={0}, timeout={1}", resource, timeout, GetType().Name);

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
            if (_lockId.HasValue)
                Release();
        }

        internal FluentNHibernateDistributedLock Acquire()
        {
            var finish = DateTime.UtcNow.Add(_timeout);

            while (!_cancellationToken.IsCancellationRequested && finish > DateTime.UtcNow)
            {
                if (CreateLockRow())
                {
                    Logger.DebugFormat("Granted distributed lock for {0}", _resource);
                    return this;
                }

                _cancellationToken.WaitHandle.WaitOne(_options.DistributedLockPollInterval);
            }

            return null;
        }

        private bool CreateLockRow()
        {
            return SqlUtil.WrapForTransaction(() => SqlUtil.WrapForDeadlock(_cancellationToken, () =>
            {
                using (var session = _storage.GetStatelessSession())
                {
                    using (var transaction = session.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var count = session.Query<_DistributedLock>()
                            .Count(i => i.Resource == _resource);
                        if (count == 0)
                        {
                            var distributedLock = new _DistributedLock
                            {
                                CreatedAt = session.Storage.UtcNow, Resource = _resource,
                                ExpireAtAsLong = session.Storage.UtcNow.Add(_timeout).ToEpochDate()
                            };
                            session.Insert(distributedLock);
                          
                            _lockId = distributedLock.Id;
                            transaction.Commit();
                            if (Logger.IsDebugEnabled())
                                Logger.DebugFormat("Created distributed lock for {0}",
                                    JsonConvert.SerializeObject(distributedLock));
                            return true;
                        }
                    }
                }

                return false;
            }, _options));
        }

        internal void Release()
        {
            SqlUtil.WrapForTransaction(() =>
            {
                SqlUtil.WrapForDeadlock(_cancellationToken, () =>
                {
                    if (_lockId.HasValue)
                        using (var session = _storage.GetStatelessSession())
                        {
                            session.Query<_DistributedLock>().Where(i => i.Id == _lockId).Delete();
                          
                            Logger.DebugFormat("Released distributed lock for {0}", _resource);
                            _lockId = null;
                        }
                }, _options);
            });
        }
    }
}