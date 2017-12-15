using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateStorageConnection : JobStorageConnection
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly string updateJobParameterValueSql =
            Helper.GetSingleFieldUpdateSql(nameof(_JobParameter), nameof(_JobParameter.Value),
                nameof(_JobParameter.Id));

        private static readonly string deleteServerSql =
            string.Format("delete from {0} where {1}=:{2}", nameof(_Server), nameof(_Server.Id),
                Helper.IdParameterName);

        private static readonly string updateServerLastHeartBeatSql =
            Helper.GetSingleFieldUpdateSql(nameof(_Server), nameof(_Server.LastHeartbeat), nameof(_Server.Id));

        private static readonly string DeleteServerByLastHeartbeatSql = string.Format(
            "delete from {0} where {1} < :{2}",
            nameof(_Server),
            nameof(_Server.LastHeartbeat), Helper.ValueParameterName);

        private readonly FluentNHibernateStorage _storage;

        public FluentNHibernateStorageConnection(FluentNHibernateStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
        }

        public override IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new FluentNHibernateWriteOnlyTransaction(_storage);
        }

        public override IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            return new FluentNHibernateDistributedLock(_storage, resource, timeout).Acquire();
        }

        public override string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt,
            TimeSpan expireIn)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (parameters == null) throw new ArgumentNullException("parameters");

            var invocationData = InvocationData.Serialize(job);

            Logger.TraceFormat("CreateExpiredJob={0}", JobHelper.ToJson(invocationData));

            return _storage.UseSession(session =>
            {
                using (var transaction = session.BeginTransaction())
                {
                    var sqlJob = new _Job
                    {
                        InvocationData = JobHelper.ToJson(invocationData),
                        Arguments = invocationData.Arguments,
                        CreatedAt = createdAt,
                        ExpireAt = createdAt.Add(expireIn)
                    };
                    session.Insert(sqlJob);
                    session.Flush();
                    foreach (var keyValuePair in parameters)
                    {
                        session.Insert(new _JobParameter
                        {
                            Job = sqlJob,
                            Name = keyValuePair.Key,
                            Value = keyValuePair.Value
                        });
                    }
                    session.Flush();

                    transaction.Commit();
                    return sqlJob.Id.ToString();
                }
            });
        }

        public override IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null || queues.Length == 0) throw new ArgumentNullException("queues");

            var providers = queues
                .Select(queue => _storage.QueueProviders.GetProvider(queue))
                .Distinct()
                .ToArray();

            if (providers.Length != 1)
            {
                throw new InvalidOperationException(string.Format(
                    "Multiple provider instances registered for queues: {0}. You should choose only one type of persistent queues per server instance.",
                    string.Join(", ", queues)));
            }

            var persistentQueue = providers[0].GetJobQueue();
            return persistentQueue.Dequeue(queues, cancellationToken);
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (name == null) throw new ArgumentNullException("name");

            _storage.UseSession(session =>
            {
                var updated = session.CreateQuery(updateJobParameterValueSql)
                    .SetParameter(Helper.ValueParameterName, value)
                    .SetParameter(Helper.IdParameterName, int.Parse(id)).ExecuteUpdate();
                if (updated == 0)
                {
                    var jobParameter = new _JobParameter
                    {
                        Job = new _Job {Id = int.Parse(id)},
                        Name = name,
                        Value = value
                    };
                    session.Insert(jobParameter);
                }
                session.Flush();
                ;
            });
        }

        public override string GetJobParameter(string id, string name)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (name == null) throw new ArgumentNullException("name");

            return _storage.UseStatelessSession(session =>
                session.Query<_JobParameter>().Where(i => i.Job.Id == int.Parse(id) && i.Name == name)
                    .Select(i => i.Value).SingleOrDefault());
        }

        public override JobData GetJobData(string jobId)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");

            return _storage.UseSession(session =>
            {
                var jobData =
                    session
                        .Query<_Job>().FirstOrDefault(i => i.Id == int.Parse(jobId));

                if (jobData == null) return null;

                var invocationData = JobHelper.FromJson<InvocationData>(jobData.InvocationData);
                invocationData.Arguments = jobData.Arguments;

                Job job = null;
                JobLoadException loadException = null;

                try
                {
                    job = invocationData.Deserialize();
                }
                catch (JobLoadException ex)
                {
                    loadException = ex;
                }

                return new JobData
                {
                    Job = job,
                    State = jobData.CurrentState?.Name,
                    CreatedAt = jobData.CreatedAt,
                    LoadException = loadException
                };
            });
        }

        public override StateData GetStateData(string jobId)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");

            return _storage.UseStatelessSession(session =>
            {
                var jobState = session.Query<_Job>().Where(i => i.Id == int.Parse(jobId)).Select(i => i.CurrentState)
                    .FirstOrDefault();
                if (jobState == null)
                {
                    return null;
                }


                var data = new Dictionary<string, string>(
                    JobHelper.FromJson<Dictionary<string, string>>(jobState.Data),
                    StringComparer.OrdinalIgnoreCase);

                return new StateData
                {
                    Name = jobState.Name,
                    Reason = jobState.Reason,
                    Data = data
                };
            });
        }

        public override void AnnounceServer(string serverId, ServerContext context)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (context == null) throw new ArgumentNullException("context");

            _storage.UseStatelessSession(session =>
            {
                session.UpsertEntity<_Server>(i => i.Id == serverId,
                    i =>
                    {
                        i.Data = JobHelper.ToJson(new ServerData
                        {
                            WorkerCount = context.WorkerCount,
                            Queues = context.Queues,
                            StartedAt = DateTime.UtcNow
                        });
                        i.LastHeartbeat = DateTime.UtcNow;
                    }, i => { i.Id = serverId; });
            });
        }

        public override void RemoveServer(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");

            _storage.UseStatelessSession(session =>
            {
                session.CreateQuery(deleteServerSql).SetParameter(Helper.IdParameterName, serverId).ExecuteUpdate();
            });
        }

        public override void Heartbeat(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");

            _storage.UseStatelessSession(session =>
            {
                session.CreateQuery(updateServerLastHeartBeatSql)
                    .SetParameter(Helper.ValueParameterName, DateTime.UtcNow)
                    .SetParameter(Helper.IdParameterName, serverId)
                    .ExecuteUpdate();
            });
        }

        public override int RemoveTimedOutServers(TimeSpan timeOut)
        {
            if (timeOut.Duration() != timeOut)
            {
                throw new ArgumentException("The `timeOut` value must be positive.", "timeOut");
            }

            return
                _storage.UseStatelessSession(session =>
                    session.CreateQuery(DeleteServerByLastHeartbeatSql)
                        .SetParameter(Helper.ValueParameterName, DateTime.UtcNow.Add(timeOut.Negate()))
                        .ExecuteUpdate()
                );
        }

        public override long GetSetCount(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return
                _storage.UseStatelessSession(session =>
                    session.Query<_Set>().Count(i => i.Key == key));
        }

        public override List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
        {
            if (key == null) throw new ArgumentNullException("key");
            return _storage.UseStatelessSession(session =>
                {
                    return session.Query<_Set>().OrderBy(i => i.Id).Skip(startingFrom)
                        .Take(endingAt - startingFrom + 1).Select(i => i.Value).ToList();
                }
            );
        }

        public override HashSet<string> GetAllItemsFromSet(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return
                _storage.UseStatelessSession(session =>
                {
                    var result = session.Query<_Set>().Where(i => i.Key == key).OrderBy(i => i.Id)
                        .Select(i => i.Value).ToList();
                    return new HashSet<string>(result);
                });
        }

        public override string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (toScore < fromScore)
                throw new ArgumentException("The `toScore` value must be higher or equal to the `fromScore` value.");

            return
                _storage.UseStatelessSession(session =>
                    session.Query<_Set>().OrderBy(i => i.Score)
                        .Where(i => i.Key == key && i.Score >= fromScore && i.Score <= toScore).Select(i => i.Value)
                        .SingleOrDefault());
        }

        public override long GetCounter(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            return
                _storage
                    .UseStatelessSession(session =>
                    {
                        return session.Query<_Counter>().Where(i => i.Key == key).Sum(i => i.Value) +
                               session.Query<_AggregatedCounter>().Where(i => i.Key == key).Sum(i => i.Value);
                    });
        }

        public override long GetHashCount(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return
                _storage
                    .UseStatelessSession(session =>
                        session.Query<_Hash>().Count(i => i.Key == key));
        }

        public override TimeSpan GetHashTtl(string key)
        {
            return GetTTL<_Hash>(key);
        }

        public override long GetListCount(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return
                _storage
                    .UseStatelessSession(session =>
                        session.Query<_List>().Count(i => i.Key == key));
        }

        public override TimeSpan GetListTtl(string key)
        {
            return GetTTL<_List>(key);
        }

        public override string GetValueFromHash(string key, string name)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (name == null) throw new ArgumentNullException("name");

            return
                _storage
                    .UseStatelessSession(session =>
                        session.Query<_Hash>().Where(i => i.Key == key && i.Field == name).Select(i => i.Value)
                            .FirstOrDefault());
        }

        public override List<string> GetRangeFromList(string key, int startingFrom, int endingAt)
        {
            if (key == null) throw new ArgumentNullException("key");
            return _storage.UseStatelessSession(session =>
            {
                return
                    session.Query<_List>().OrderByDescending(i => i.Id).Where(i => i.Key == key)
                        .Select(i => i.Value).Skip(startingFrom).Take(endingAt - startingFrom + 1).ToList();

                ;
            });
        }

        public override List<string> GetAllItemsFromList(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return _storage.UseStatelessSession(session =>
            {
                return
                    session.Query<_List>().OrderByDescending(i => i.Id).Where(i => i.Key == key)
                        .Select(i => i.Value).ToList();

                ;
            });
        }

        private TimeSpan GetTTL<T>(string key) where T : IExpireWithKey
        {
            if (key == null) throw new ArgumentNullException("key");

            return _storage.UseStatelessSession(session =>
            {
                var a = session.Query<_List>().Where(i => i.Key == key).Min(i => i.ExpireAt);
                if (a == null)
                {
                    return TimeSpan.FromSeconds(-1);
                }
                return a.Value - DateTime.UtcNow;
            });
        }

        public override TimeSpan GetSetTtl(string key)
        {
            return GetTTL<_Set>(key);
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            _storage.UseTransactionStateless(session =>
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    session.UpsertEntity<_Hash>(i => i.Key == key && i.Field == keyValuePair.Key,
                        i => { i.Value = keyValuePair.Value; }, i =>
                        {
                            i.Key = key;
                            i.Field = keyValuePair.Key;
                        });
                }
            });
        }

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return _storage.UseStatelessSession(session =>
            {
                var result = session.Query<_Hash>().Where(i => i.Key == key)
                    .ToDictionary(i => i.Field, i => i.Value);
                return result.Count != 0 ? result : null;
            });
        }
    }
}