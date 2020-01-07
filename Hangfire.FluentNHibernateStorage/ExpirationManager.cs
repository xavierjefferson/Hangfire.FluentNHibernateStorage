using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.FluentNHibernateStorage
{
#pragma warning disable 618
    public class ExpirationManager : IBackgroundProcess, IServerComponent
    {
        internal const string DistributedLockKey = "expirationmanager";

        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        internal static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);
        internal static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromSeconds(1);

        private readonly FluentNHibernateJobStorage _storage;

        public ExpirationManager(FluentNHibernateJobStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Execute(BackgroundProcessContext context)
        {
            var cancellationToken = context.CancellationToken;
            Execute(cancellationToken);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            DateTime cutOffDate;
            using (var session = _storage.GetSession())
            {
                cutOffDate = session.Storage.UtcNow;
            }

            var actionQueue = new ExpirationActionQueue(_storage, cutOffDate, cancellationToken);
            actionQueue.EnqueueDeleteJobDetailEntity<_JobState>();
            actionQueue.EnqueueDeleteJobDetailEntity<_JobQueue>();
            actionQueue.EnqueueDeleteJobDetailEntity<_JobParameter>();


            actionQueue.EnqueueDeleteEntities<_DistributedLock>((session, cutoff) =>
            {
                var unixDate = cutoff.ToUnixDate();
                var idList = session.Query<_DistributedLock>()
                    .Where(i => i.ExpireAtAsLong < unixDate)
                    .Select(i => i.Id)
                    .ToList();
                return session.DeleteByInt32Id<_DistributedLock>(idList);
            });
            actionQueue.EnqueueDeleteExpirableEntity<_AggregatedCounter>();
            actionQueue.EnqueueDeleteExpirableEntity<_Job>();
            actionQueue.EnqueueDeleteExpirableEntity<_List>();
            actionQueue.EnqueueDeleteExpirableEntity<_Set>();
            actionQueue.EnqueueDeleteExpirableEntity<_Hash>();

            actionQueue.Run();

            cancellationToken.WaitHandle.WaitOne(_storage.Options.JobExpirationCheckInterval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
#pragma warning restore 618
    }
}