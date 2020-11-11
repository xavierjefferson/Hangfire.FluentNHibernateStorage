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
        private static readonly ILog Logger = LogProvider.For<CountersAggregator>();
        private readonly string _tableName;
        private readonly FluentNHibernateJobStorage _storage;

        public CountersAggregator(FluentNHibernateJobStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _tableName = _storage.GetTableName<_Counter>();
        }

        public void Execute(BackgroundProcessContext context)
        {
            Execute(context.StoppedToken);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Logger.DebugFormat("Aggregating records in '{0}' table", _tableName);

            long removedCount = int.MaxValue;
            while (removedCount >= NumberOfRecordsInSinglePass && !cancellationToken.IsCancellationRequested)
                _storage.UseStatelessSession(session =>
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

                        foreach (var item in countersByName)
                            session.UpsertEntity<_AggregatedCounter>(i => i.Key == item.Key && i.Value == item.value,
                                n =>
                                {
                                    n.ExpireAt = item.expireAt;
                                    n.Value += item.value;
                                }, n => { n.Key = item.Key; });

                        removedCount =
                            session.DeleteByInt32Id<_Counter>(counters.Select(counter => counter.Id).ToArray());

                        transaction.Commit();
                    }
                });
            Logger.DebugFormat("Done aggregating records in '{1}' table.  Waiting {0}",
                _storage.Options.CountersAggregateInterval, _tableName);

            cancellationToken.WaitHandle.WaitOne(_storage.Options.CountersAggregateInterval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
#pragma warning restore 618
    }
}