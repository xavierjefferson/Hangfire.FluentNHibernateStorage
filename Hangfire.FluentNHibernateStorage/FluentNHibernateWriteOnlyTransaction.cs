using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    internal class FluentNHibernateWriteOnlyTransaction : JobStorageTransaction
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly string updatez =
            Helper.singlefieldupdate(nameof(_Job), nameof(_Job.ExpireAt), nameof(_Job.Id));

        private static readonly string uz =
            Helper.singlefieldupdate(nameof(_Job), nameof(_Job.ExpireAt), nameof(_Job.Id));

        private readonly Queue<Action<ISession>> _commandQueue
            = new Queue<Action<ISession>>();

        private readonly FluentNHibernateStorage _storage;

        public FluentNHibernateWriteOnlyTransaction(FluentNHibernateStorage storage)
        {
            if (storage == null) throw new ArgumentNullException("storage");

            _storage = storage;
        }

        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            Logger.TraceFormat("ExpireJob jobId={0}", jobId);

            AcquireJobLock();

            QueueCommand(x =>
                x.CreateQuery(updatez).SetParameter("id", int.Parse(jobId))
                    .SetParameter("value", DateTime.UtcNow.Add(expireIn)).ExecuteUpdate());
        }

        public override void PersistJob(string jobId)
        {
            Logger.TraceFormat("PersistJob jobId={0}", jobId);

            AcquireJobLock();

            QueueCommand(x =>
                x.CreateQuery(uz).SetParameter("value", null).SetParameter("id", int.Parse(jobId)).ExecuteUpdate());
        }

        public override void SetJobState(string jobId, IState state)
        {
            Logger.TraceFormat("SetJobState jobId={0}", jobId);

            AcquireStateLock();
            AcquireJobLock();
            QueueCommand(x =>
            {
                var job = x.Query<_Job>().FirstOrDefault(i => i.Id == int.Parse(jobId));
                var sqlState = new _JobState
                {
                    Job = job,
                    Reason = state.Reason,
                    Name = state.Name,
                    CreatedAt = DateTime.UtcNow,
                    Data = JobHelper.ToJson(state.SerializeData())
                };
                x.Save(sqlState);
                x.Flush();
                job.StateName = state.Name;
                job.State = sqlState;
                x.Save(job);
            });
        }

        public override void AddJobState(string jobId, IState state)
        {
            Logger.TraceFormat("AddJobState jobId={0}, state={1}", jobId, state);

            AcquireStateLock();
            QueueCommand(x =>
            {
                x.Save(new _JobState
                {
                    Job = new _Job {Id = int.Parse(jobId)},
                    Name = state.Name,
                    Reason = state.Reason,
                    CreatedAt = DateTime.UtcNow,
                    Data = JobHelper.ToJson(state.SerializeData())
                });
            });
        }

        public override void AddToQueue(string queue, string jobId)
        {
            Logger.TraceFormat("AddToQueue jobId={0}", jobId);

            var provider = _storage.QueueProviders.GetProvider(queue);
            var persistentQueue = provider.GetJobQueue();

            QueueCommand(x => persistentQueue.Enqueue(x, queue, jobId));
        }

        public override void IncrementCounter(string key)
        {
            Logger.TraceFormat("IncrementCounter key={0}", key);

            AcquireCounterLock();
            QueueCommand(x => x.Save(new _Counter {Key = key, Value = 1}));
        }


        public override void IncrementCounter(string key, TimeSpan expireIn)
        {
            Logger.TraceFormat("IncrementCounter key={0}, expireIn={1}", key, expireIn);

            AcquireCounterLock();
            QueueCommand(x => x.Save(new _Counter {Key = key, Value = 1, ExpireAt = DateTime.UtcNow.Add(expireIn)}));
        }

        public override void DecrementCounter(string key)
        {
            Logger.TraceFormat("DecrementCounter key={0}", key);

            AcquireCounterLock();
            QueueCommand(x => x.Save(new _Counter {Key = key, Value = -1}));
        }

        public override void DecrementCounter(string key, TimeSpan expireIn)
        {
            Logger.TraceFormat("DecrementCounter key={0} expireIn={1}", key, expireIn);

            AcquireCounterLock();
            QueueCommand(x => x.Save(new _Counter {Key = key, Value = -1, ExpireAt = DateTime.UtcNow.Add(expireIn)}));
        }

        public override void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0.0);
        }

        public override void AddToSet(string key, string value, double score)
        {
            Logger.TraceFormat("AddToSet key={0} value={1}", key, value);

            AcquireSetLock();
            QueueCommand(x =>
            {
                x.Upsert<_Set>(i => i.Key == key && i.Value == value, i => i.Score = score, i =>
                {
                    i.Key = key;
                    i.Value = value;
                });
            });
        }

        public override void AddRangeToSet(string key, IList<string> items)
        {
            Logger.TraceFormat("AddRangeToSet key={0}", key);

            if (key == null) throw new ArgumentNullException("key");
            if (items == null) throw new ArgumentNullException("items");

            AcquireSetLock();
            QueueCommand(x =>
            {
                foreach (var i in items)
                {
                    x.Save(new _Set {Key = key, Value = i, Score = 0});
                }
            });
        }


        public override void RemoveFromSet(string key, string value)
        {
            Logger.TraceFormat("RemoveFromSet key={0} value={1}", key, value);

            AcquireSetLock();
            QueueCommand(x => x.DeleteByExpression<_Set>(0, i => i.Key == key && i.Value == value));
        }

        public override void ExpireSet(string key, TimeSpan expireIn)
        {
            Logger.TraceFormat("ExpireSet key={0} expirein={1}", key, expireIn);

            if (key == null) throw new ArgumentNullException("key");

            AcquireSetLock();
            QueueCommand(x =>
                x.UpdateByExpression<_Set>(i => i.Key == key, i => i.ExpireAt = DateTime.UtcNow.Add(expireIn)));
        }

        public override void InsertToList(string key, string value)
        {
            Logger.TraceFormat("InsertToList key={0} value={1}", key, value);

            AcquireListLock();
            QueueCommand(x => x.Save(new _List {Key = key, Value = value}));
        }


        public override void ExpireList(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException("key");

            Logger.TraceFormat("ExpireList key={0} expirein={1}", key, expireIn);

            AcquireListLock();
            QueueCommand(x =>
                x.UpdateByExpression<_List>(i => i.Key == key, i => i.ExpireAt = DateTime.UtcNow.Add(expireIn)));
        }

        public override void RemoveFromList(string key, string value)
        {
            Logger.TraceFormat("RemoveFromList key={0} value={1}", key, value);

            AcquireListLock();
            QueueCommand(x => x.DeleteByExpression<_List>(0, i => i.Key == key && i.Value == value));
        }

        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            Logger.TraceFormat("TrimList key={0} from={1} to={2}", key, keepStartingFrom, keepEndingAt);

            AcquireListLock();
            QueueCommand(x => x.Delete().Execute(
                @"
delete lst
from List lst
	inner join (SELECT tmp.Id, @rownum := @rownum + 1 AS rank
		  		FROM List tmp, 
       				(SELECT @rownum := 0) r ) ranked on ranked.Id = lst.Id
where lst.Key = @key
    and ranked.rank not between @start and @end",
                new {key, start = keepStartingFrom + 1, end = keepEndingAt + 1}));
        }

        public override void PersistHash(string key)
        {
            Logger.TraceFormat("PersistHash key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireHashLock();
            QueueCommand(x => x.UpdateByExpression<_Hash>(i => i.Key == key, i => i.ExpireAt = null));
        }

        public override void PersistSet(string key)
        {
            Logger.TraceFormat("PersistSet key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireSetLock();
            QueueCommand(x => x.UpdateByExpression<_Set>(i => i.Key == key, i => i.ExpireAt = null));
        }

        public override void RemoveSet(string key)
        {
            Logger.TraceFormat("RemoveSet key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireSetLock();
            QueueCommand(x => x.DeleteByExpression<_Set>(0, i => i.Key == key));
        }

        public override void PersistList(string key)
        {
            Logger.TraceFormat("PersistList key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireListLock();
            QueueCommand(x => x.UpdateByExpression<_List>(i => i.Key == key, i => i.ExpireAt = null));
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            Logger.TraceFormat("SetRangeInHash key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            AcquireHashLock();
            QueueCommand(x =>
            {
                foreach (var m in keyValuePairs)
                {
                    x.Upsert<_Hash>(i => i.Key == key && i.Field == m.Key, i => i.Value = m.Value,
                        i =>
                        {
                            i.Field = m.Key;
                            i.Key = key;
                        }
                    );
                }
            });
        }

        public override void ExpireHash(string key, TimeSpan expireIn)
        {
            Logger.TraceFormat("ExpireHash key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireHashLock();
            QueueCommand(x =>
                x.UpdateByExpression<_Hash>(i => i.Key == key, i => i.ExpireAt = DateTime.UtcNow.Add(expireIn)));
        }

        public override void RemoveHash(string key)
        {
            Logger.TraceFormat("RemoveHash key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireHashLock();
            QueueCommand(x => x.DeleteByExpression<_Hash>(0, i => i.Key == key));
        }

        public override void Commit()
        {
            _storage.UseTransaction(connection =>
            {
                foreach (var command in _commandQueue)
                {
                    command(connection);
                    connection.Flush();
                }
            });
        }

        internal void QueueCommand(Action<ISession> action)
        {
            _commandQueue.Enqueue(action);
        }

        private void AcquireJobLock()
        {
            AcquireLock("Job");
        }

        private void AcquireSetLock()
        {
            AcquireLock("Set");
        }

        private void AcquireListLock()
        {
            AcquireLock("List");
        }

        private void AcquireHashLock()
        {
            AcquireLock("Hash");
        }

        private void AcquireStateLock()
        {
            AcquireLock("State");
        }

        private void AcquireCounterLock()
        {
            AcquireLock("Counter");
        }

        private void AcquireLock(string resource)
        {
        }
    }
}