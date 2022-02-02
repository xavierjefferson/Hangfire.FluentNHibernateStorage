using System;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.FluentNHibernateStorage
{
#pragma warning disable 618
    public class CountersAggregator : IBackgroundProcess, IServerComponent
    {
        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromMilliseconds(500);

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

        private DateTime? MaxOfDateTimes(DateTime? a, DateTime? b)
        {
            if (a == null) return b;
            if (b == null) return a;
            return a.Value > b.Value ? a.Value : b.Value;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Logger.DebugFormat("Aggregating records in '{0}' table", _tableName);

            long removedCount = 0;
            do
            {
                _storage.UseStatelessSessionInTransaction(session =>
                {
                    var counters = session.Query<_Counter>().Take(NumberOfRecordsInSinglePass).ToList();
                    if (counters.Any())
                    {
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
                            session.UpsertEntity<_AggregatedCounter>(i => i.Key == item.Key,
                                n =>
                                {
                                    n.ExpireAt = MaxOfDateTimes(n.ExpireAt, item.expireAt);
                                    n.Value += item.value;
                                }, n => { n.Key = item.Key; });

                        removedCount =
                            session.DeleteByInt32Id<_Counter>(counters.Select(counter => counter.Id)
                                .ToArray());
                    }
                });
                if (removedCount >= NumberOfRecordsInSinglePass)
                {
                    cancellationToken.Wait(DelayBetweenPasses);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (removedCount >= NumberOfRecordsInSinglePass);

            Logger.TraceFormat($"Records from the '{_tableName}' aggregated.");

            cancellationToken.Wait(_storage.Options.CountersAggregateInterval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
#pragma warning restore 618
    }
}