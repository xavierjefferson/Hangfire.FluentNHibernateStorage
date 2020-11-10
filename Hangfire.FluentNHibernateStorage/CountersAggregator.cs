using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.FluentNHibernateStorage
{
#pragma warning disable 618
    public class CountersAggregator : IBackgroundProcess, IServerComponent
    {
        private const int NumberOfRecordsInSinglePass = 1000;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromMilliseconds(500);

        private readonly FluentNHibernateJobStorage _storage;

        public CountersAggregator(FluentNHibernateJobStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Execute(BackgroundProcessContext context)
        {
            Execute(context.CancellationToken);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Logger.Info("Aggregating records in 'Counter' table");

            long removedCount = int.MaxValue;
            while (removedCount >= NumberOfRecordsInSinglePass && !cancellationToken.IsCancellationRequested)
                _storage.UseSession(session =>
                {
                    using (var transaction = session.BeginTransaction())
                    {
                        var counters = session.Query<_Counter>().Take(NumberOfRecordsInSinglePass).ToList();
                        var countersByName = counters.GroupBy(counter => counter.Key)
                            .Select(i =>
                                new
                                {
                                    i.Key,
                                    value = i.Sum(counter => counter.Value),
                                    expireAt = i.Max(counter => counter.ExpireAt)
                                })
                            .ToList();
                        var query = session.CreateQuery(SqlUtil.UpdateAggregateCounterStatement);

                        //TODO: Consider using upsert approach
                        foreach (var item in countersByName)
                        {
                            if (item.expireAt.HasValue)
                                query.SetParameter(SqlUtil.ValueParameter2Name, item.expireAt.Value);
                            else
                                query.SetParameter(SqlUtil.ValueParameter2Name, null);

                            if (query.SetString(SqlUtil.IdParameterName, item.Key)
                                .SetParameter(SqlUtil.ValueParameterName, item.value)
                                .ExecuteUpdate() == 0)
                                session.Insert(new _AggregatedCounter
                                {
                                    Key = item.Key,
                                    Value = item.value,
                                    ExpireAt = item.expireAt
                                });
                        }

                        removedCount =
                            session.DeleteByInt32Id<_Counter>(counters.Select(counter => counter.Id).ToArray());

                        transaction.Commit();
                    }
                });
            Logger.InfoFormat("Done aggregating records in 'Counter' table.  Waiting {0}",
                _storage.Options.CountersAggregateInterval);

            cancellationToken.WaitHandle.WaitOne(_storage.Options.CountersAggregateInterval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
#pragma warning restore 618
    }
}