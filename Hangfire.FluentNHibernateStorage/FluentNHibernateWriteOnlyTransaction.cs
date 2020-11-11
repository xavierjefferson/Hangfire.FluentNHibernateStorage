using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateWriteOnlyTransaction : JobStorageTransaction
    {
        private static readonly ILog Logger = LogProvider.For<FluentNHibernateWriteOnlyTransaction>();


        //transactional command queue.
        private readonly Queue<Action<StatelessSessionWrapper>> _commandQueue
            = new Queue<Action<StatelessSessionWrapper>>();

        private readonly FluentNHibernateJobStorage _storage;

        public FluentNHibernateWriteOnlyTransaction(FluentNHibernateJobStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        private void SetExpireAt<T>(string key, DateTime? expire, StatelessSessionWrapper session) where T : IExpirableWithKey
        {
            var queryString = SqlUtil.SetExpireAtByKeyStatementDictionary[typeof(T)];
            session.CreateQuery(queryString)
                .SetParameter(SqlUtil.ValueParameterName, expire)
                .SetParameter(SqlUtil.IdParameterName, key)
                .ExecuteUpdate();
            //does nothing
        }

        private void DeleteByKey<T>(string key, StatelessSessionWrapper session) where T : IExpirableWithKey
        {
            session.Query<T>().Where(i => i.Key == key).Delete();
            //does nothing
        }

        private void DeleteByKeyValue<T>(string key, string value, StatelessSessionWrapper session) where T : IKeyWithStringValue
        {
            session.Query<T>().Where(i => i.Key == key && i.Value == value).Delete();
            //does nothing
        }

        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            Logger.DebugFormat("ExpireJob jobId={0}", jobId);
            var converter = StringToInt32Converter.Convert(jobId);
            if (!converter.Valid) return;

            AcquireJobLock();

            QueueCommand(session =>
                session.CreateQuery(SqlUtil.UpdateJobExpireAtStatement)
                    .SetParameter(SqlUtil.IdParameterName, converter.Value)
                    .SetParameter(SqlUtil.ValueParameterName, session.Storage.UtcNow.Add(expireIn))
                    .ExecuteUpdate());
        }

        public override void PersistJob(string jobId)
        {
            Logger.DebugFormat("PersistJob jobId={0}", jobId);
            var converter = StringToInt32Converter.Convert(jobId);
            if (!converter.Valid) return;

            AcquireJobLock();

            QueueCommand(session =>
                session.CreateQuery(SqlUtil.UpdateJobExpireAtStatement)
                    .SetParameter(SqlUtil.ValueParameterName, null)
                    .SetParameter(SqlUtil.IdParameterName, converter.Value)
                    .ExecuteUpdate());
        }

        public override void SetJobState(string jobId, IState state)
        {
            Logger.DebugFormat("SetJobState jobId={0}", jobId);
            var converter = StringToInt32Converter.Convert(jobId);
            if (!converter.Valid) return;

            AcquireStateLock();
            AcquireJobLock();
            QueueCommand(session =>
            {
                var job = session.Query<_Job>().SingleOrDefault(i => i.Id == converter.Value);
                if (job != null)
                {
                    var jobState = new _JobState
                    {
                        Job = job,
                        Reason = state.Reason,
                        Name = state.Name,
                        CreatedAt = session.Storage.UtcNow,
                        Data = SerializationHelper.Serialize(state.SerializeData())
                    };
                    session.Insert(jobState);
                

                    job.StateData = jobState.Data;
                    job.StateReason = jobState.Reason;
                    job.StateName = jobState.Name;
                    job.LastStateChangedAt = session.Storage.UtcNow;

                    session.Update(job);
                    //does nothing
                }
            });
        }

        public override void AddJobState(string jobId, IState state)
        {
            Logger.DebugFormat("AddJobState jobId={0}, state={1}", jobId, state);
            var converter = StringToInt32Converter.Convert(jobId);
            if (!converter.Valid) return;

            AcquireStateLock();
            QueueCommand(session =>
            {
                session.Insert(new _JobState
                {
                    Job = new _Job {Id = converter.Value},
                    Name = state.Name,
                    Reason = state.Reason,
                    CreatedAt = session.Storage.UtcNow,
                    Data = SerializationHelper.Serialize(state.SerializeData())
                });
            });
        }

        public override void AddToQueue(string queue, string jobId)
        {
            Logger.DebugFormat("AddToQueue jobId={0}", jobId);

            var provider = _storage.QueueProviders.GetProvider(queue);
            var persistentQueue = provider.GetJobQueue();

            QueueCommand(session => persistentQueue.Enqueue(session, queue, jobId));
        }

        public override void IncrementCounter(string key)
        {
            InsertCounter(key, 1);
        }


        public override void IncrementCounter(string key, TimeSpan expireIn)
        {
            InsertCounter(key, 1, expireIn);
        }

        private void InsertCounter(string key, int value, TimeSpan? expireIn = null)
        {
            Logger.DebugFormat("InsertCounter key={0}, expireIn={1}", key, expireIn);

            AcquireCounterLock();
            QueueCommand(session =>
                session.Insert(new _Counter
                {
                    Key = key,
                    Value = value,
                    ExpireAt = expireIn == null ? (DateTime?) null : session.Storage.UtcNow.Add(expireIn.Value)
                }));
        }

        public override void DecrementCounter(string key)
        {
            InsertCounter(key, -1);
        }

        public override void DecrementCounter(string key, TimeSpan expireIn)
        {
            InsertCounter(key, -1, expireIn);
        }

        public override void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0.0);
        }

        public override void AddToSet(string key, string value, double score)
        {
            Logger.DebugFormat("AddToSet key={0} value={1}", key, value);

            AcquireSetLock();
            QueueCommand(session =>
            {
                session.UpsertEntity<_Set>(i => i.Key == key && i.Value == value, i => i.Score = score, i =>
                {
                    i.Key = key;
                    i.Value = value;
                });
            });
        }

        public override void AddRangeToSet(string key, IList<string> items)
        {
            Logger.DebugFormat("AddRangeToSet key={0}", key);

            if (key == null) throw new ArgumentNullException(nameof(key));
            if (items == null) throw new ArgumentNullException(nameof(items));

            AcquireSetLock();
            QueueCommand(session =>
            {
                foreach (var i in items) session.Insert(new _Set {Key = key, Value = i, Score = 0});
            });
        }


        public override void RemoveFromSet(string key, string value)
        {
            Logger.DebugFormat("RemoveFromSet key={0} value={1}", key, value);

            AcquireSetLock();
            QueueCommand(session => { DeleteByKeyValue<_Set>(key, value, session); });
        }

        public override void ExpireSet(string key, TimeSpan expireIn)
        {
            Logger.DebugFormat("ExpireSet key={0} expirein={1}", key, expireIn);

            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireSetLock();
            QueueCommand(session => { SetExpireAt<_Set>(key, session.Storage.UtcNow.Add(expireIn), session); });
        }

        public override void InsertToList(string key, string value)
        {
            Logger.DebugFormat("InsertToList key={0} value={1}", key, value);

            AcquireListLock();
            QueueCommand(session => session.Insert(new _List {Key = key, Value = value}));
        }


        public override void ExpireList(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            Logger.DebugFormat("ExpireList key={0} expirein={1}", key, expireIn);

            AcquireListLock();
            QueueCommand(session => { SetExpireAt<_List>(key, session.Storage.UtcNow.Add(expireIn), session); });
        }

        public override void RemoveFromList(string key, string value)
        {
            Logger.DebugFormat("RemoveFromList key={0} value={1}", key, value);

            AcquireListLock();
            QueueCommand(session => { DeleteByKeyValue<_List>(key, value, session); });
        }

        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            Logger.DebugFormat("TrimList key={0} from={1} to={2}", key, keepStartingFrom, keepEndingAt);

            AcquireListLock();
            QueueCommand(session =>
            {
                var idList = session.Query<_List>()
                    .OrderBy(i => i.Id)
                    .Where(i => i.Key == key).ToList()
                    .Select((i, j) => new {index = j, id = i.Id}).ToList();
                var before = idList.Where(i => i.index < keepStartingFrom || i.index > keepEndingAt)
                    .Select(i => i.id)
                    .ToList();
                session.DeleteByInt32Id<_List>(before);
            });
        }

        public override void PersistHash(string key)
        {
            Logger.DebugFormat("PersistHash key={0} ", key);

            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireHashLock();
            QueueCommand(session => { SetExpireAt<_Hash>(key, null, session); });
        }

        public override void PersistSet(string key)
        {
            Logger.DebugFormat("PersistSet key={0} ", key);

            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireSetLock();
            QueueCommand(session => { SetExpireAt<_Set>(key, null, session); });
        }

        public override void RemoveSet(string key)
        {
            Logger.DebugFormat("RemoveSet key={0} ", key);

            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireSetLock();
            QueueCommand(session => { DeleteByKey<_Set>(key, session); });
        }

        public override void PersistList(string key)
        {
            Logger.DebugFormat("PersistList key={0} ", key);

            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireListLock();
            QueueCommand(session => { SetExpireAt<_List>(key, null, session); });
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            Logger.DebugFormat("SetRangeInHash key={0} ", key);

            if (key == null) throw new ArgumentNullException(nameof(key));
            if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

            AcquireHashLock();
            QueueCommand(session =>
            {
                foreach (var keyValuePair in keyValuePairs)
                    session.UpsertEntity<_Hash>(i => i.Key == key && i.Field == keyValuePair.Key,
                        i => i.Value = keyValuePair.Value,
                        i =>
                        {
                            i.Field = keyValuePair.Key;
                            i.Key = key;
                        }
                    );
            });
        }

        public override void ExpireHash(string key, TimeSpan expireIn)
        {
            Logger.DebugFormat("ExpireHash key={0} ", key);

            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireHashLock();
            QueueCommand(session => { SetExpireAt<_Hash>(key, session.Storage.UtcNow.Add(expireIn), session); });
        }

        public override void RemoveHash(string key)
        {
            Logger.DebugFormat("RemoveHash key={0} ", key);

            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireHashLock();
            QueueCommand(session => { DeleteByKey<_Hash>(key, session); });
        }

        public override void Commit()
        {
            _storage.UseStatelessTransaction( session =>
            {
                foreach (var command in _commandQueue)
                {
                    command(session);
                    //does nothing
                }
            });
        }

        internal void QueueCommand(Action<StatelessSessionWrapper> action)
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