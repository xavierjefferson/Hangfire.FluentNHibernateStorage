using System;
using System.Linq;
using System.Transactions;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.JobQueue
{
    public class FluentNHibernateJobQueueMonitoringApiTests : IClassFixture<TestDatabaseFixture>, IDisposable
    {
        public FluentNHibernateJobQueueMonitoringApiTests()
        {
            _storage = ConnectionUtils.GetStorage();
            _sut = new FluentNHibernateJobQueueMonitoringApi(_storage);
        }

        public void Dispose()
        {
            _storage.Dispose();
        }

        private readonly string _queue = "default";
        private readonly FluentNHibernateJobStorage _storage;
        private readonly FluentNHibernateJobQueueMonitoringApi _sut;

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetEnqueuedAndFetchedCount_ReturnsEqueuedCount_WhenExists()
        {
            EnqueuedAndFetchedCountDto result = null;

            _storage.UseSession(session =>
            {
                var newJob = FluentNHibernateWriteOnlyTransactionTests.InsertNewJob(session);
                session.Insert(new _JobQueue {Job = newJob, Queue = _queue});
                session.Flush();
                result = _sut.GetEnqueuedAndFetchedCount(_queue);

                session.Truncate<_JobQueue>();
            }, FluentNHibernateJobStorageSessionStateEnum.Stateful);

            Assert.Equal(1, result.EnqueuedCount);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetEnqueuedJobIds_ReturnsCorrectResult()
        {
            int[] result = null;
            _storage.UseSession(session =>
            {
                for (var i = 1; i <= 10; i++)
                {
                    var newJob = FluentNHibernateWriteOnlyTransactionTests.InsertNewJob(session);
                    session.Insert(new _JobQueue {Job = newJob, Queue = _queue});
                }
                session.Flush();
                result = _sut.GetEnqueuedJobIds(_queue, 3, 2).ToArray();

                session.Truncate<_JobQueue>();
            }, FluentNHibernateJobStorageSessionStateEnum.Stateful);

            Assert.Equal(2, result.Length);
            Assert.Equal(4, result[0]);
            Assert.Equal(5, result[1]);
        }


        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetEnqueuedJobIds_ReturnsEmptyCollection_IfQueueIsEmpty()
        {
            var result = _sut.GetEnqueuedJobIds(_queue, 5, 15);

            Assert.Empty(result);
        }
    }
}