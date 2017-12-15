using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage
{
    internal class FluentNHibernateWriteOnlyTransaction : JobStorageTransaction
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly string UpdateJobExpireAtSql =
            Helper.GetSingleFieldUpdateSql(nameof(_Job), nameof(_Job.ExpireAt), nameof(_Job.Id));

        private static readonly Dictionary<Type, string> DeleteByKeyStatementDictionary = new Dictionary<Type, string>
        {
            {typeof(_Set), GetDeleteByKeyStatement<_Set>()},
            {typeof(_Hash), GetDeleteByKeyStatement<_List>()},
            {typeof(_List), GetDeleteByKeyStatement<_List>()}
        };

        private static readonly Dictionary<Type, string> DeleteByKeyValueStatementlDictionary =
            new Dictionary<Type, string>
            {
                {typeof(_Set), GetDeleteByKeyValueStatement<_Set>()},
                {typeof(_Hash), GetDeleteByKeyValueStatement<_List>()},
                {typeof(_List), GetDeleteByKeyValueStatement<_List>()}
            };

        private static readonly Dictionary<Type, string> SetExpireStatementDictionary = new Dictionary<Type, string>
        {
            {typeof(_Set), GetSetExpireByKeyStatement<_Set>()},
            {typeof(_Hash), GetSetExpireByKeyStatement<_List>()},
            {typeof(_List), GetSetExpireByKeyStatement<_List>()}
        };


        //transactional command queue.
        private readonly Queue<Action<IWrappedSession>> _commandQueue
            = new Queue<Action<IWrappedSession>>();

        private readonly FluentNHibernateStorage _storage;

        public FluentNHibernateWriteOnlyTransaction(FluentNHibernateStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
        }

        private void SetExpireAt<T>(string key, DateTime? expire, IWrappedSession session) where T : IExpireWithKey
        {
            session.CreateQuery(SetExpireStatementDictionary[typeof(T)]).SetParameter(Helper.ValueParameterName, expire)
                .SetParameter(Helper.IdParameterName, key).ExecuteUpdate();
        }

        private void DeleteByKey<T>(string key, IWrappedSession session) where T : IExpireWithKey
        {
            session.CreateQuery(DeleteByKeyStatementDictionary[typeof(T)]).SetParameter(Helper.ValueParameterName, key)
                .ExecuteUpdate();
        }

        private void DeleteByKeyValue<T>(string key, string value, IWrappedSession session) where T : IExpireWithKey
        {
            session.CreateQuery(DeleteByKeyValueStatementlDictionary[typeof(T)])
                .SetParameter(Helper.ValueParameterName, key)
                .SetParameter(Helper.ValueParameter2Name, value).ExecuteUpdate();
        }

        private static string GetSetExpireByKeyStatement<T>() where T : IExpireWithKey
        {
            return string.Format("update `{0}` set `{1}`={2} where `{3}`:={4}", typeof(T).Name,
                nameof(IExpirable.ExpireAt), Helper.ValueParameterName, nameof(IExpireWithKey.Key),
                Helper.IdParameterName);
        }

        private static string GetDeleteByKeyStatement<T>() where T : IExpireWithKey
        {
            return string.Format("delete from `{0}` where `{1}`:={2}", typeof(T).Name, nameof(IExpireWithKey.Key),
                Helper.ValueParameterName);
        }

        private static string GetDeleteByKeyValueStatement<T>() where T : IKeyStringValue
        {
            return string.Format("delete from `{0}` where `{1}`:={2} and `{3}`=:{4}", typeof(T).Name,
                nameof(IExpireWithKey.Key),
                Helper.ValueParameterName, nameof(IKeyStringValue.Value), Helper.ValueParameter2Name);
        }

        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            Logger.TraceFormat("ExpireJob jobId={0}", jobId);

            AcquireJobLock();

            QueueCommand(session =>
                session.CreateQuery(UpdateJobExpireAtSql).SetParameter(Helper.IdParameterName, int.Parse(jobId))
                    .SetParameter(Helper.ValueParameterName, DateTime.UtcNow.Add(expireIn)).ExecuteUpdate());
        }

        public override void PersistJob(string jobId)
        {
            Logger.TraceFormat("PersistJob jobId={0}", jobId);

            AcquireJobLock();

            QueueCommand(session =>
                session.CreateQuery(UpdateJobExpireAtSql).SetParameter(Helper.ValueParameterName, null)
                    .SetParameter(Helper.IdParameterName, int.Parse(jobId))
                    .ExecuteUpdate());
        }

        public override void SetJobState(string jobId, IState state)
        {
            Logger.TraceFormat("SetJobState jobId={0}", jobId);

            AcquireStateLock();
            AcquireJobLock();
            QueueCommand(session =>
            {
                var job = session.Query<_Job>().FirstOrDefault(i => i.Id == int.Parse(jobId));
                if (job != null)
                {
                    var sqlState = new _JobState
                    {
                        Job = job,
                        Reason = state.Reason,
                        Name = state.Name,
                        CreatedAt = DateTime.UtcNow,
                        Data = JobHelper.ToJson(state.SerializeData())
                    };
                    session.Insert(sqlState);
                    session.Flush();

                    job.CurrentState = sqlState;
                    session.Update(job);
                    session.Flush();
                }
            });
        }

        public override void AddJobState(string jobId, IState state)
        {
            Logger.TraceFormat("AddJobState jobId={0}, state={1}", jobId, state);

            AcquireStateLock();
            QueueCommand(session =>
            {
                session.Insert(new _JobState
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
            Logger.TraceFormat("InsertCounter key={0}, expireIn={1}", key, expireIn);

            AcquireCounterLock();
            QueueCommand(session =>
                session.Insert(new _Counter
                {
                    Key = key,
                    Value = value,
                    ExpireAt = expireIn == null ? (DateTime?) null : DateTime.UtcNow.Add(expireIn.Value)
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
            Logger.TraceFormat("AddToSet key={0} value={1}", key, value);

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
            Logger.TraceFormat("AddRangeToSet key={0}", key);

            if (key == null) throw new ArgumentNullException("key");
            if (items == null) throw new ArgumentNullException("items");

            AcquireSetLock();
            QueueCommand(session =>
            {
                foreach (var i in items)
                {
                    session.Insert(new _Set {Key = key, Value = i, Score = 0});
                }
            });
        }


        public override void RemoveFromSet(string key, string value)
        {
            Logger.TraceFormat("RemoveFromSet key={0} value={1}", key, value);

            AcquireSetLock();
            QueueCommand(session => { DeleteByKeyValue<_Set>(key, value, session); });
        }

        public override void ExpireSet(string key, TimeSpan expireIn)
        {
            Logger.TraceFormat("ExpireSet key={0} expirein={1}", key, expireIn);

            if (key == null) throw new ArgumentNullException("key");

            AcquireSetLock();
            QueueCommand(session => { SetExpireAt<_Set>(key, DateTime.UtcNow.Add(expireIn), session); });
        }

        public override void InsertToList(string key, string value)
        {
            Logger.TraceFormat("InsertToList key={0} value={1}", key, value);

            AcquireListLock();
            QueueCommand(session => session.Insert(new _List {Key = key, Value = value}));
        }


        public override void ExpireList(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException("key");

            Logger.TraceFormat("ExpireList key={0} expirein={1}", key, expireIn);

            AcquireListLock();
            QueueCommand(session => { SetExpireAt<_List>(key, DateTime.UtcNow.Add(expireIn), session); });
        }

        public override void RemoveFromList(string key, string value)
        {
            Logger.TraceFormat("RemoveFromList key={0} value={1}", key, value);

            AcquireListLock();
            QueueCommand(session => { DeleteByKeyValue<_List>(key, value, session); });
        }

        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            Logger.TraceFormat("TrimList key={0} from={1} to={2}", key, keepStartingFrom, keepEndingAt);

            AcquireListLock();
            QueueCommand(session =>
            {
                var idList = session.Query<_List>().OrderBy(i => i.Id).Where(i => i.Key == key)
                    .Select((i, j) => new {index = j, id = i.Id});
                var before = idList.Where(i => i.index < keepStartingFrom)
                    .Union(idList.Where(i => i.index > keepEndingAt))
                    .Select(i => i.id).ToList();
                session.DeleteById<_List>(before);
            });
        }

        public override void PersistHash(string key)
        {
            Logger.TraceFormat("PersistHash key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireHashLock();
            QueueCommand(session => { SetExpireAt<_Hash>(key, null, session); });
        }

        public override void PersistSet(string key)
        {
            Logger.TraceFormat("PersistSet key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireSetLock();
            QueueCommand(session => { SetExpireAt<_Set>(key, null, session); });
        }

        public override void RemoveSet(string key)
        {
            Logger.TraceFormat("RemoveSet key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireSetLock();
            QueueCommand(session => { DeleteByKey<_Set>(key, session); });
        }

        public override void PersistList(string key)
        {
            Logger.TraceFormat("PersistList key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireListLock();
            QueueCommand(session => { SetExpireAt<_List>(key, null, session); });
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            Logger.TraceFormat("SetRangeInHash key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            AcquireHashLock();
            QueueCommand(session =>
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    session.UpsertEntity<_Hash>(i => i.Key == key && i.Field == keyValuePair.Key,
                        i => i.Value = keyValuePair.Value,
                        i =>
                        {
                            i.Field = keyValuePair.Key;
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
            QueueCommand(session => { SetExpireAt<_Hash>(key, DateTime.UtcNow.Add(expireIn), session); });
        }

        public override void RemoveHash(string key)
        {
            Logger.TraceFormat("RemoveHash key={0} ", key);

            if (key == null) throw new ArgumentNullException("key");

            AcquireHashLock();
            QueueCommand(session => { DeleteByKey<_Hash>(key, session); });
        }

        public override void Commit()
        {
            _storage.UseStatefulTransaction(session =>
            {
                foreach (var command in _commandQueue)
                {
                    command(session);
                    session.Flush();
                }
            });
        }

        internal void QueueCommand(Action<IWrappedSession> action)
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