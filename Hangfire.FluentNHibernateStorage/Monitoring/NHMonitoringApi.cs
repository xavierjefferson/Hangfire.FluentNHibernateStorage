using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage.Monitoring
{
    internal class NHMonitoringApi : IMonitoringApi
    {
        private readonly int? _jobListLimit;
        private readonly NHStorage _storage;

        public NHMonitoringApi([NotNull] NHStorage storage, int? jobListLimit)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
            _jobListLimit = jobListLimit;
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            var tuples = _storage.QueueProviders
                .Select(x => x.GetJobQueueMonitoringApi())
                .SelectMany(x => x.GetQueues(), (monitoring, queue) => new {Monitoring = monitoring, Queue = queue})
                .OrderBy(x => x.Queue)
                .ToArray();

            var result = new List<QueueWithTopEnqueuedJobsDto>(tuples.Length);

            foreach (var tuple in tuples)
            {
                var enqueuedJobIds = tuple.Monitoring.GetEnqueuedJobIds(tuple.Queue, 0, 5);
                var counters = tuple.Monitoring.GetEnqueuedAndFetchedCount(tuple.Queue);

                var firstJobs = UseConnection(connection => EnqueuedJobs(connection, enqueuedJobIds));

                result.Add(new QueueWithTopEnqueuedJobsDto
                {
                    Name = tuple.Queue,
                    Length = counters.EnqueuedCount ?? 0,
                    Fetched = counters.FetchedCount,
                    FirstJobs = firstJobs
                });
            }

            return result;
        }

        public IList<ServerDto> Servers()
        {
            return UseConnection<IList<ServerDto>>(connection =>
            {
                var result = new List<ServerDto>();

                foreach (var server in connection.Query<Entities._Server>())
                {
                    var data = JobHelper.FromJson<ServerData>(server.Data);
                    result.Add(new ServerDto
                    {
                        Name = server.Id.ToString(),
                        Heartbeat = server.LastHeartbeat,
                        Queues = data.Queues,
                        StartedAt = data.StartedAt.HasValue ? data.StartedAt.Value : DateTime.MinValue,
                        WorkersCount = data.WorkerCount
                    });
                }

                return result;
            });
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            return UseConnection(connection =>
            {
                var job = connection.Query<_Job>().SingleOrDefault(i => i.Id == int.Parse(jobId));
                if (job == null) return null;

                var parameters = job.Parameters.ToDictionary(x => x.Name, x => x.Value);
                var history =
                    job.SqlStates.OrderByDescending(i => i.Id)
                        .Select(x => new StateHistoryDto
                        {
                            StateName = x.Name,
                            CreatedAt = x.CreatedAt,
                            Reason = x.Reason,
                            Data = new Dictionary<string, string>(
                                JobHelper.FromJson<Dictionary<string, string>>(x.Data),
                                StringComparer.OrdinalIgnoreCase)
                        })
                        .ToList();

                return new JobDetailsDto
                {
                    CreatedAt = job.CreatedAt,
                    ExpireAt = job.ExpireAt,
                    Job = DeserializeJob(job.InvocationData, job.Arguments),
                    History = history,
                    Properties = parameters
                };
            });
        }

        public StatisticsDto GetStatistics()
        {
            var statistics =
                UseConnection(connection =>
                    {
                        var a = connection.Query<_Job>().GroupBy(i => i.StateName)
                            .Select(i => new {i.Key, c = i.Count()}).ToDictionary(i => i.Key, j => j.c);

                        int GetJobStatusCount(string b)
                        {
                            if (a.ContainsKey(b))
                            {
                                return a[b];
                            }
                            return 0;
                        }

                        long CountStats(string key)
                        {
                            return connection.Query<_AggregatedCounter>().Where(i => i.Key == key).Sum(i => i.Value) +
                                   connection.Query<_Counter>().Where(i => i.Key == key).Sum(i => i.Value);
                        }

                        return new StatisticsDto
                        {
                            Enqueued = GetJobStatusCount("Enqueued"),
                            Failed = GetJobStatusCount("Failed"),
                            Processing = GetJobStatusCount("Processing"),
                            Scheduled = GetJobStatusCount("Scheduled"),
                            Servers = connection.Query<Entities._Server>().Count(),
                            Succeeded = CountStats("stats:succeeded"),
                            Deleted = CountStats("stats:deleted"),
                            Recurring = connection.Query<_Set>().Count(i => i.Key == "recurring-jobs'")
                        };
                    }
                );

            statistics.Queues = _storage.QueueProviders
                .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
                .Count();

            return statistics;
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, from, perPage);

            return UseConnection(connection => EnqueuedJobs(connection, enqueuedJobIds));
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage);

            return UseConnection(connection => FetchedJobs(connection, fetchedJobIds));
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from, count,
                ProcessingState.StateName,
                (sqlJob, job, stateData) => new ProcessingJobDto
                {
                    Job = job,
                    ServerId = stateData.ContainsKey("ServerId") ? stateData["ServerId"] : stateData["ServerName"],
                    StartedAt = JobHelper.DeserializeDateTime(stateData["StartedAt"])
                }));
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from, count,
                ScheduledState.StateName,
                (sqlJob, job, stateData) => new ScheduledJobDto
                {
                    Job = job,
                    EnqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]),
                    ScheduledAt = JobHelper.DeserializeDateTime(stateData["ScheduledAt"])
                }));
        }

        public JobList<SucceededJobDto> SucceededJobs(int from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from,
                count,
                SucceededState.StateName,
                (sqlJob, job, stateData) => new SucceededJobDto
                {
                    Job = job,
                    Result = stateData.ContainsKey("Result") ? stateData["Result"] : null,
                    TotalDuration = stateData.ContainsKey("PerformanceDuration") && stateData.ContainsKey("Latency")
                        ? (long?) long.Parse(stateData["PerformanceDuration"]) +
                          (long?) long.Parse(stateData["Latency"])
                        : null,
                    SucceededAt = JobHelper.DeserializeNullableDateTime(stateData["SucceededAt"])
                }));
        }

        public JobList<FailedJobDto> FailedJobs(int from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from,
                count,
                FailedState.StateName,
                (sqlJob, job, stateData) => new FailedJobDto
                {
                    Job = job,
                    Reason = sqlJob.StateReason,
                    ExceptionDetails = stateData["ExceptionDetails"],
                    ExceptionMessage = stateData["ExceptionMessage"],
                    ExceptionType = stateData["ExceptionType"],
                    FailedAt = JobHelper.DeserializeNullableDateTime(stateData["FailedAt"])
                }));
        }

        public JobList<DeletedJobDto> DeletedJobs(int from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from,
                count,
                DeletedState.StateName,
                (sqlJob, job, stateData) => new DeletedJobDto
                {
                    Job = job,
                    DeletedAt = JobHelper.DeserializeNullableDateTime(stateData["DeletedAt"])
                }));
        }

        public long ScheduledCount()
        {
            return UseConnection(connection =>
                GetNumberOfJobsByStateName(connection, ScheduledState.StateName));
        }

        public long EnqueuedCount(string queue)
        {
            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            return counters.EnqueuedCount ?? 0;
        }

        public long FetchedCount(string queue)
        {
            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            return counters.FetchedCount ?? 0;
        }

        public long FailedCount()
        {
            return UseConnection(connection =>
                GetNumberOfJobsByStateName(connection, FailedState.StateName));
        }

        public long ProcessingCount()
        {
            return UseConnection(connection =>
                GetNumberOfJobsByStateName(connection, ProcessingState.StateName));
        }

        public long SucceededListCount()
        {
            return UseConnection(connection =>
                GetNumberOfJobsByStateName(connection, SucceededState.StateName));
        }

        public long DeletedListCount()
        {
            return UseConnection(connection =>
                GetNumberOfJobsByStateName(connection, DeletedState.StateName));
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return UseConnection(connection =>
                GetTimelineStats(connection, "succeeded"));
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return UseConnection(connection =>
                GetTimelineStats(connection, "failed"));
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, "succeeded"));
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, "failed"));
        }

        private T UseConnection<T>(Func<ISession, T> action)
        {
            return _storage.UseTransaction(action, IsolationLevel.ReadUncommitted);
        }

        private long GetNumberOfJobsByStateName(ISession connection, string stateName)
        {
            var count = connection.Query<_Job>().Count(i => i.StateName == stateName);
            if (_jobListLimit.HasValue)
            {
                return Math.Max(count, _jobListLimit.Value);
            }
            return count;
        }

        private IPersistentJobQueueMonitoringApi GetQueueApi(string queueName)
        {
            var provider = _storage.QueueProviders.GetProvider(queueName);
            var monitoringApi = provider.GetJobQueueMonitoringApi();

            return monitoringApi;
        }

        private JobList<TDto> GetJobs<TDto>(
            ISession connection,
            int from,
            int count,
            string stateName,
            Func<_Job, Job, Dictionary<string, string>, TDto> selector)
        {
            var jobsSql =
                @"select * from (
  select j.*, s.Reason as StateReason, s.Data as StateData, @rownum := @rownum + 1 AS rank
  from Job j
    cross join (SELECT @rownum := 0) r
  left join State s on j.StateId = s.Id
  where j.StateName = @stateName
  order by j.Id desc
) as j where j.rank between @start and @end ";

            var jobs =
                connection.Query<_Job>(
                        jobsSql,
                        new {stateName, start = from + 1, end = from + count})
                    .ToList();

            return DeserializeJobs(jobs, selector);
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<_Job> jobs,
            Func<_Job, Job, Dictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                var deserializedData = JobHelper.FromJson<Dictionary<string, string>>(job.StateData);
                var stateData = deserializedData != null
                    ? new Dictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                    : null;

                var dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);

                result.Add(new KeyValuePair<string, TDto>(
                    job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        private static Job DeserializeJob(string invocationData, string arguments)
        {
            var data = JobHelper.FromJson<InvocationData>(invocationData);
            data.Arguments = arguments;

            try
            {
                return data.Deserialize();
            }
            catch (JobLoadException)
            {
                return null;
            }
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            ISession connection,
            string type)
        {
            var endDate = DateTime.UtcNow.Date;
            var dates = new List<DateTime>();
            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddDays(-1);
            }

            var keyMaps = dates.ToDictionary(x => string.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd")),
                x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(ISession connection,
            IDictionary<string, DateTime> keyMaps)
        {
            var valuesMap = connection.Query<_AggregatedCounter>().Where(i => keyMaps.Keys.Contains(i.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
            }

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        private JobList<EnqueuedJobDto> EnqueuedJobs(
            ISession connection,
            IEnumerable<int> jobIds)
        {
            var enqueuedJobsSql =
                @"select j.*, s.Reason as StateReason, s.Data as StateData 
from Job j
left join State s on s.Id = j.StateId
where j.Id in @jobIds";

            var jobs = connection.Query<_Job>().Where(i => jobIds.Contains(i.Id)).ToList();

            return DeserializeJobs(
                jobs,
                (sqlJob, job, stateData) => new EnqueuedJobDto
                {
                    Job = job,
                    State = sqlJob.StateName,
                    EnqueuedAt = sqlJob.StateName == EnqueuedState.StateName
                        ? JobHelper.DeserializeNullableDateTime(stateData["EnqueuedAt"])
                        : null
                });
        }

        private JobList<FetchedJobDto> FetchedJobs(
            ISession connection,
            IEnumerable<int> jobIds)
        {
            var result = new List<KeyValuePair<string, FetchedJobDto>>();

            foreach (var job in connection.Query<_Job>().Where(i => jobIds.Contains(i.Id)))
            {
                result.Add(new KeyValuePair<string, FetchedJobDto>(
                    job.Id.ToString(),
                    new FetchedJobDto
                    {
                        Job = DeserializeJob(job.InvocationData, job.Arguments),
                        State = job.StateName
                    }));
            }

            return new JobList<FetchedJobDto>(result);
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(
            ISession connection,
            string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => string.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH")),
                x => x);

            return GetTimelineStats(connection, keyMaps);
        }
    }
}