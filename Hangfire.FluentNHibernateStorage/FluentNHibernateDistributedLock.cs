using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Storage;
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

        private FluentNHibernateDistributedLock(FluentNHibernateJobStorage storage, string resource, TimeSpan timeout,
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

        public static FluentNHibernateDistributedLock Acquire(FluentNHibernateJobStorage storage, string resource,
            TimeSpan timeout,
            CancellationToken? cancellationToken = null)
        {
            var tmp = new FluentNHibernateDistributedLock(storage, resource, timeout, cancellationToken);
            tmp.Initialize();
            return tmp;
        }

        private T TryLock<T>(Func<T> funcTaken, Func<T> notTaken)
        {
            var timeout = _options.QueuePollInterval;
            var lockTaken = false;

            try
            {
                Monitor.TryEnter(Mutex, timeout, ref lockTaken);
                if (lockTaken)
                    return funcTaken();
                else
                    return notTaken();
            }
            finally
            {
                // Ensure that the lock is released.
                if (lockTaken) Monitor.Exit(Mutex);
            }
        }

        internal void Initialize()
        {
            var started = Stopwatch.StartNew();

            do
            {
                if (SqlUtil.WrapForTransaction(() => SqlUtil.WrapForDeadlock(_cancellationToken, () =>
                {
                    var done = TryLock(() =>
                    {
                        var result = false;
                        _storage.UseStatelessSessionInTransaction(session =>
                        {
                            var distributedLock = session.Query<_DistributedLock>()
                                .FirstOrDefault(i => i.Resource == _resource);

                            var utcNow = session.Storage.UtcNow;
                            var expireAtAsLong = utcNow.Add(_timeout).ToEpochDate();
                            if (distributedLock == null)
                            {
                                distributedLock = new _DistributedLock
                                {
                                    CreatedAt = utcNow,
                                    Resource = _resource,
                                    ExpireAtAsLong = expireAtAsLong
                                };
                                session.Insert(distributedLock);

                                _lockId = distributedLock.Id;
                                result = true;
                                if (Logger.IsDebugEnabled())
                                    Logger.Debug($"Inserted row for distributed lock '{_resource}'");
                            }
                            else if (distributedLock.ExpireAtAsLong < utcNow.ToEpochDate())
                            {
                                distributedLock.CreatedAt = utcNow;
                                distributedLock.ExpireAtAsLong = expireAtAsLong;
                                session.Update(distributedLock);
                                if (Logger.IsDebugEnabled())
                                    Logger.Debug($"Re-used row for distributed lock '{_resource}'");
                                _lockId = distributedLock.Id;
                                result = true;
                            }
                        });
                        return result;
                    }, () => false);
                    return done;
                }, _options)))
                {
                    if (Logger.IsDebugEnabled())
                        Logger.DebugFormat("Granted distributed lock '{0}'", _resource);
                    return;
                }

                if (started.Elapsed > _timeout) break;
                if (Logger.IsDebugEnabled())
                    Logger.Debug(
                        $"Will poll for distributed lock '{_resource}' in {_options.DistributedLockPollInterval}.");
                _cancellationToken.Wait(_options.DistributedLockPollInterval);
                _cancellationToken.ThrowIfCancellationRequested();
            } while (true);

            //dont change this.  Hangfire looks for resource name in exception properties
            throw new DistributedLockTimeoutException(_resource);
        }


        internal void Release()
        {
            SqlUtil.WrapForTransaction(() =>
            {
                SqlUtil.WrapForDeadlock(_cancellationToken, () =>
                {
                    lock (Mutex)
                    {
                        if (_lockId.HasValue)
                            _storage.UseStatelessSession(session =>
                            {
                                session.Query<_DistributedLock>().Where(i => i.Id == _lockId).Delete();

                                Logger.DebugFormat("Released distributed lock for {0}", _resource);
                                _lockId = null;
                            });
                    }
                }, _options);
            });
        }
    }
}