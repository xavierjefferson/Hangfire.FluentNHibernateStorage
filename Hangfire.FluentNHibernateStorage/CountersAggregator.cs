using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.FluentNHibernateStorage
{
    public class CountersAggregator : IBackgroundProcess
    {
        private const int NumberOfRecordsInSinglePass = 1000;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromMilliseconds(500);

        private static readonly string UpdateAggregateCounterSql = string.Format(
            "update `{0}` s set s.`{1}`=s.`{1}` + :{4}, s.`{3}`= case when s.`{3}` >  :{6} then s.`{3}` else :{6} end where s.`{2}` = :{5}",
            nameof(_AggregatedCounter), nameof(_AggregatedCounter.Value), nameof(_AggregatedCounter.Key),
            nameof(_AggregatedCounter.ExpireAt), Helper.ValueParameterName, Helper.IdParameterName,
            Helper.ValueParameter2Name);

        private readonly TimeSpan _interval;

        private readonly FluentNHibernateJobStorage _storage;

        public CountersAggregator(FluentNHibernateJobStorage storage, TimeSpan interval)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
            _interval = interval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            GetValue(context.CancellationToken);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            var token = cancellationToken;
            GetValue(token);
        }

        private void GetValue(CancellationToken token)
        {
            Logger.Info("Aggregating records in 'Counter' table...");

            long removedCount = 0;

            do
            {
                _storage.UseStatelessSession(connection =>
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        var counters = connection.Query<_Counter>().Take(NumberOfRecordsInSinglePass).ToList();
                        var countersByName = counters.GroupBy(counter => counter.Key).Select(i =>
                            new
                            {
                                i.Key,
                                value = i.Sum(counter => counter.Value),
                                expireAt = i.Max(counter => counter.ExpireAt)
                            }).ToList();
                        var query = connection.CreateQuery(UpdateAggregateCounterSql);
                        foreach (var item in countersByName)
                        {
                            if (item.expireAt.HasValue)
                            {
                                query.SetParameter(Helper.ValueParameter2Name, item.expireAt.Value);
                            }
                            else
                            {
                                query.SetParameter(Helper.ValueParameter2Name, null);
                            }
                            if (query.SetString(Helper.IdParameterName, item.Key)
                                    .SetParameter(Helper.ValueParameterName, item.value)
                                    .ExecuteUpdate() == 0)
                            {
                                connection.Insert(new _AggregatedCounter
                                {
                                    Key = item.Key,
                                    Value = item.value,
                                    ExpireAt = item.expireAt
                                });
                            }
                            ;
                        }
                        removedCount = connection.DeleteByInt32Id<_Counter>(counters.Select(iz => iz.Id).ToArray());

                        transaction.Commit();
                    }
                });

                if (removedCount >= NumberOfRecordsInSinglePass)
                {
                    token.WaitHandle.WaitOne(DelayBetweenPasses);
                    token.ThrowIfCancellationRequested();
                }
            } while (removedCount >= NumberOfRecordsInSinglePass);

            token.WaitHandle.WaitOne(_interval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
    }
}