using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    internal class CountersAggregator : IServerComponent
    {
        private const int NumberOfRecordsInSinglePass = 1000;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromMilliseconds(500);

        private static readonly string UpdateAggregateCounterSql = string.Format(
            "update `{0}` s set s.`{1}`=s.`{1}` + :{4}, s.`{3}`=:{6} where s.`{2}` = :{5}",
            nameof(_AggregatedCounter), nameof(_AggregatedCounter.Value), nameof(_AggregatedCounter.Key),
            nameof(_AggregatedCounter.ExpireAt), Helper.ValueParameterName, Helper.IdParameterName, Helper.ValueParameter2Name);

        //private static readonly string UpdateAggregateCounterSql = string.Format(
        //    "update `{0}` s set s.`{3}`=:{6} where s.`{2}` = :{5}",
        //    nameof(_AggregatedCounter), nameof(_AggregatedCounter.Value), nameof(_AggregatedCounter.Id),
        //    nameof(_AggregatedCounter.ExpireAt), Helper.ValueParameterName, Helper.IdParameterName, Helper.ValueParameter2Name);

        private static readonly string DeleteCounterByIdsSql = string.Format("delete from `{0}` where `{1}` in (:ids)",
            nameof(_Counter),
            nameof(_Counter.Id));

        private readonly TimeSpan _interval;

        private readonly FluentNHibernateStorage _storage;

        public CountersAggregator(FluentNHibernateStorage storage, TimeSpan interval)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
            _interval = interval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Logger.DebugFormat("Aggregating records in 'Counter' table...");

           long removedCount = 0;

            do
            {
                _storage.UseStatelessSession(connection =>
                {
                    using (var i = connection.BeginTransaction())
                    {
                        var m = connection.Query<_Counter>().Take(NumberOfRecordsInSinglePass).ToList();
                        var c = m.GroupBy(iz => iz.Key).Select(iz =>
                            new {id = iz.Key, value = iz.Sum(im => im.Value), expireAt = iz.Max(k => k.ExpireAt)}).ToList();
                        var q = connection.CreateQuery(UpdateAggregateCounterSql);
                        foreach (var item in c)
                        {
                            //if (q.SetParameter(Helper.ValueParameterName, item.value).SetParameter(Helper.ValueParameter2Name, item.expireAt)
                            //        .SetParameter(Helper.IdParameterName, item.id)
                            //        .ExecuteUpdate() == 0)
                            if (item.expireAt.HasValue)
                            {
                                q.SetParameter(Helper.ValueParameter2Name, item.expireAt.Value);

                            }
                            else
                            {
                                q.SetParameter(Helper.ValueParameter2Name, null);
                            }
                                if (q.SetString(Helper.IdParameterName, item.id).SetParameter(Helper.ValueParameterName, item.value)
                                    .ExecuteUpdate() == 0)
                            {
                                connection.Insert(new _AggregatedCounter
                                {
                                    Key = item.id,
                                    Value = item.value,
                                    ExpireAt = item.expireAt
                                });
                            }
                            ;
                        }
                        removedCount = m.Any()
                            ? connection.DeleteById<_Counter>(m.Select(iz => iz.Id).ToArray())
                                
                            : 0;
                        i.Commit();
                    }
                });

                if (removedCount >= NumberOfRecordsInSinglePass)
                {
                    cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (removedCount >= NumberOfRecordsInSinglePass);

            cancellationToken.WaitHandle.WaitOne(_interval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
    }
}