using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Providers;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue
{
    public abstract class FluentNHibernateJobQueueMonitoringApiTests<T, U> : TestBase<T, U> where T : IDbProvider, new() where U : TestDatabaseFixture, IDisposable
  
    {
        protected FluentNHibernateJobQueueMonitoringApiTests()
        {
            _storage = GetStorage();
            _sut = new FluentNHibernateJobQueueMonitoringApi(_storage);
        }

        private readonly string _queue = "default";
        private readonly FluentNHibernateJobStorage _storage;
        private readonly FluentNHibernateJobQueueMonitoringApi _sut;

        [Fact]
        public void GetEnqueuedAndFetchedCount_ReturnsEqueuedCount_WhenExists()
        {
            EnqueuedAndFetchedCountDto result = null;

            _storage.UseStatelessSession(session =>
            {
                var newJob = JobInsertionHelper.InsertNewJob(session);
                session.Insert(new _JobQueue {Job = newJob, Queue = _queue});

                result = _sut.GetEnqueuedAndFetchedCount(_queue);

                session.DeleteAll<_JobQueue>();
            });

            Assert.Equal(1, result.EnqueuedCount);
        }

        [Fact]
        public void GetEnqueuedJobIds_ReturnsCorrectResult()
        {
            long[] result = null;
            var jobs = new List<_Job>();
            _storage.UseStatelessSession(session =>
            {
                for (var i = 1; i <= 10; i++)
                {
                    var newJob = JobInsertionHelper.InsertNewJob(session);
                    jobs.Add(newJob);
                    session.Insert(new _JobQueue {Job = newJob, Queue = _queue});
                }

                //does nothing
                result = _sut.GetEnqueuedJobIds(_queue, 3, 2).ToArray();

                session.DeleteAll<_JobQueue>();
            });

            Assert.Equal(2, result.Length);
            Assert.Equal(jobs[3].Id, result[0]);
            Assert.Equal(jobs[4].Id, result[1]);
        }


        [Fact]
        public void GetEnqueuedJobIds_ReturnsEmptyCollection_IfQueueIsEmpty()
        {
            var result = _sut.GetEnqueuedJobIds(_queue, 5, 15);

            Assert.Empty(result);
        }
    }
}