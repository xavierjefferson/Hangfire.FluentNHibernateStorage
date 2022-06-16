using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue
{
    public abstract class JobQueueMonitoringApiTests : TestBase, IDisposable

    {
        protected JobQueueMonitoringApiTests(DatabaseFixtureBase fixture) : base(fixture)
        {
            var storage = GetStorage();

            _api = new FluentNHibernateJobQueueMonitoringApi(storage);
        }

        private readonly string _queue = "default";
        private readonly FluentNHibernateJobQueueMonitoringApi _api;

        [Fact]
        public void GetEnqueuedAndFetchedCount_ReturnsEqueuedCount_WhenExists()
        {
            EnqueuedAndFetchedCountDto result = null;

            UseJobStorageConnectionWithSession((session,connection) =>
            {
                var newJob = InsertNewJob(session);
                session.Insert(new _JobQueue {Job = newJob, Queue = _queue});

                result = _api.GetEnqueuedAndFetchedCount(_queue);

                session.DeleteAll<_JobQueue>();
            });

            Assert.Equal(1, result.EnqueuedCount);
        }

        [Fact]
        public void GetEnqueuedJobIds_ReturnsCorrectResult()
        {
            long[] result = null;
            var jobs = new List<_Job>();
            UseJobStorageConnectionWithSession((session,connection) =>
            {
                for (var i = 1; i <= 10; i++)
                {
                    var newJob = InsertNewJob(session);
                    jobs.Add(newJob);
                    session.Insert(new _JobQueue {Job = newJob, Queue = _queue});
                }

                //does nothing
                result = _api.GetEnqueuedJobIds(_queue, 3, 2).ToArray();

                session.DeleteAll<_JobQueue>();
            });

            Assert.Equal(2, result.Length);
            Assert.Equal(jobs[3].Id, result[0]);
            Assert.Equal(jobs[4].Id, result[1]);
        }


        [Fact]
        public void GetEnqueuedJobIds_ReturnsEmptyCollection_IfQueueIsEmpty()
        {
            var result = _api.GetEnqueuedJobIds(_queue, 5, 15);

            Assert.Empty(result);
        }
    }
}