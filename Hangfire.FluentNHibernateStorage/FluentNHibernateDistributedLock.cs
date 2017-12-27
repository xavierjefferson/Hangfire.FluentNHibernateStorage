using System;
using System.Data;
using System.Threading;
using Hangfire.Logging;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLock : IDisposable, IComparable
    {
        private const int DelayBetweenPasses = 100;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly string _resource;
        private readonly FluentNHibernateJobStorage _storage;
        private readonly TimeSpan _timeout;
        private bool _acquired;
        private CancellationToken _cancellationToken;

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

            var mySqlDistributedLock = obj as FluentNHibernateDistributedLock;
            if (mySqlDistributedLock != null)
                return string.Compare(_resource, mySqlDistributedLock._resource,
                    StringComparison.InvariantCultureIgnoreCase);

            throw new ArgumentException(string.Format("Object is not a {0}", GetType().Name));
        }

        public void Dispose()
        {
            if (_acquired)
            {
                Release();
            }
        }

        internal FluentNHibernateDistributedLock Acquire()
        {
            var finish = DateTime.Now.Add(_timeout);

            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();


                if (SqlUtil.WrapForTransaction(() =>
                {
                    using (var session = _storage.GetSession())
                    {
                        using (var transaction = session.BeginTransaction(IsolationLevel.Serializable))
                        {
                            var realnow1 = session.Storage.UtcNow;
                            var count = session.CreateQuery(SqlUtil.GetCreateDistributedLockStatement())
                                .SetParameter(SqlUtil.ResourceParameterName, _resource)
                                .SetParameter(SqlUtil.ExpireAtAsLongParameterName,
                                    realnow1.Add(_timeout).ToUnixDate())
                                .SetParameter(SqlUtil.NowParameterName, realnow1)
                                .SetParameter(SqlUtil.NowAsLongParameterName, realnow1.ToUnixDate());


                            if (count.ExecuteUpdate() > 0)
                            {
                                transaction.Commit();
                                return true;
                            }
                        }
                    }
                    return false;
                }))
                {
                    Logger.DebugFormat("Granted distributed lock for {0}", _resource);
                    _acquired = true;
                    return this;
                }

                if (finish > DateTime.Now)
                {
                    _cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                }
                else
                {
                    throw new FluentNHibernateDistributedLockException("cannot acquire lock");
                }
            }
        }

        internal void Release()
        {
            SqlUtil.WrapForTransaction(() =>
            {
                using (var session = _storage.GetSession())
                {
                    session.CreateQuery(SqlUtil.DeleteDistributedLockSql)
                        .SetParameter(SqlUtil.IdParameterName, _resource)
                        .ExecuteUpdate();
                    session.Flush();
                    Logger.DebugFormat("Released distributed lock for {0}", _resource);
                }
            });
        }
    }
}