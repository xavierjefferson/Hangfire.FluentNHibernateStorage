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
        private readonly TimeSpan _interval;

        private readonly NHStorage _storage;

        public CountersAggregator(NHStorage storage, TimeSpan interval)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
            _interval = interval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Logger.DebugFormat("Aggregating records in 'Counter' table...");

            var removedCount = 0;

            do
            {
                _storage.UseConnection(connection =>
                {
                    using (var i = connection.BeginTransaction())
                    {
                        var upd = string.Format("update {0} set {1}={1}+:value, {3}=:exp where {2}=:id",
                            nameof(_AggregatedCounter), nameof(_AggregatedCounter.Value), nameof(_AggregatedCounter.Id),
                            nameof(_AggregatedCounter.ExpireAt));
                        var del = string.Format("delete from {0} where {1} in (:ids)", nameof(_Counter),
                            nameof(_Counter.Id));
                        var m = connection.Query<_Counter>().Take(NumberOfRecordsInSinglePass).ToList();
                        var c = m.GroupBy(iz => iz.Key).Select(iz =>
                            new {iz.Key, v = iz.Sum(im => im.Value), c = iz.Max(k => k.ExpireAt)}).ToList();
                        var q = connection.CreateQuery(upd);
                        foreach (var item in c)
                        {
                            if (q.SetParameter("value", item.v).SetParameter("exp", item.c).SetParameter("id", item.Key)
                                    .ExecuteUpdate() == 0)
                            {
                                connection.Save(new _AggregatedCounter
                                {
                                    Key = item.Key,
                                    Value = item.v,
                                    ExpireAt = item.c
                                });
                            }
                            ;
                        }
                        removedCount = connection.CreateQuery(del).SetParameterList("ids", m.Select(iz => iz.Id))
                            .ExecuteUpdate();
                        connection.Flush();
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