using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue
{
    public abstract class FluentNHibernateJobQueueTestsBase : TestBase
    {
        public FluentNHibernateJobQueueTestsBase(TestDatabaseFixture fixture) : base(fixture)
        {

            _storage = GetStorage();
        }

        private static readonly string[] DefaultQueues = {"default"};
        private readonly FluentNHibernateJobStorage _storage;

        private static CancellationToken CreateTimingOutCancellationToken()
        {
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return source.Token;
        }

        private static CancellationToken CreateLongTimingOutCancellationToken()
        {
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            return source.Token;
        }

        private static FluentNHibernateJobQueue CreateJobQueue(FluentNHibernateJobStorage storage)
        {
            return new FluentNHibernateJobQueue(storage);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateJobQueue(null));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Dequeue_ShouldDeleteAJob()
        {
            // Arrange
            _storage.UseStatelessSession(session =>
            {
                session.DeleteAll<_JobQueue>();
                session.DeleteAll<_Job>();
                var newjob = JobInsertionHelper.InsertNewJob(session);
                session.Insert(new _JobQueue {Job = newjob, Queue = "default"});
                //does nothing
                var queue = CreateJobQueue(_storage);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateLongTimingOutCancellationToken());

                payload.RemoveFromQueue();

                // Assert
                Assert.NotNull(payload);

                var jobInQueue = session.Query<_JobQueue>().SingleOrDefault();
                Assert.Null(jobInQueue);
            });
        }

        [Fact]
        public void Dequeue_ShouldFetchAJob_FromTheSpecifiedQueue()
        {
            // Arrange
            _storage.UseStatelessSession(session =>
            {
                var newJob = JobInsertionHelper.InsertNewJob(session);
                var newJobQueue = new _JobQueue {Job = newJob, Queue = "default"};
                session.Insert(newJobQueue);


                var queue = CreateJobQueue(_storage);

                // Act
                var payload = (FluentNHibernateFetchedJob) queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.Equal(newJob.Id.ToString(), payload.JobId);
                Assert.Equal("default", payload.Queue);
            });
        }

        [Fact]
        public void Dequeue_ShouldFetchATimedOutJobs_FromTheSpecifiedQueue()
        {
            // Arrange
            _storage.UseStatelessSession(session =>
            {
                var newJob = JobInsertionHelper.InsertNewJob(session);
                session.Insert(new _JobQueue
                {
                    Job = newJob,
                    FetchedAt = _storage.UtcNow.AddDays(-1),
                    Queue = "default"
                });
                //does nothing
                var queue = CreateJobQueue(_storage);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateLongTimingOutCancellationToken());

                // Assert
                Assert.NotEmpty(payload.JobId);
            });
        }

        [Fact]
        public void Dequeue_ShouldFetchJobs_FromMultipleQueues()
        {
            _storage.UseStatelessSession(session =>
            {
                var queueNames = new[] {"critical", "default"};
                foreach (var queueName in queueNames)
                {
                    var newJob = JobInsertionHelper.InsertNewJob(session);
                    session.Insert(new _JobQueue
                    {
                        Job = newJob,
                        Queue = queueName
                    });
                }

                //does nothing


                var queue = CreateJobQueue(_storage);


                var critical = (FluentNHibernateFetchedJob) queue.Dequeue(
                    queueNames,
                    CreateLongTimingOutCancellationToken());

                Assert.NotNull(critical.JobId);
                Assert.Equal("critical", critical.Queue);

                var @default = (FluentNHibernateFetchedJob) queue.Dequeue(
                    queueNames,
                    CreateLongTimingOutCancellationToken());

                Assert.NotNull(@default.JobId);
                Assert.Equal("default", @default.Queue);
            });
        }

        [Fact]
        public void Dequeue_ShouldFetchJobs_OnlyFromSpecifiedQueues()
        {
            _storage.UseStatelessSession(session =>
                {
                    session.DeleteAll<_JobQueue>();
                    session.DeleteAll<_Job>();
                    var newJob = JobInsertionHelper.InsertNewJob(session);
                    session.Insert(new _JobQueue
                    {
                        Job = newJob,
                        Queue = "critical"
                    });
                    //does nothing

                    var queue = CreateJobQueue(_storage);

                    Assert.Throws<OperationCanceledException>(
                        () => queue.Dequeue(
                            DefaultQueues,
                            CreateTimingOutCancellationToken()));
                });
        }

        [Fact]
        public void Dequeue_ShouldSetFetchedAt_OnlyForTheFetchedJob()
        {
            _storage.UseStatelessSession(session =>
            {
                // Arrange
                session.DeleteAll<_JobQueue>();
                session.DeleteAll<_Job>();
                for (var i = 0; i < 2; i++)
                {
                    var newJob = JobInsertionHelper.InsertNewJob(session);
                    session.Insert(new _JobQueue
                    {
                        Job = newJob,
                        Queue = "default"
                    });
                }

                //does nothing

                var queue = CreateJobQueue(_storage);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                var otherJobFetchedAt = session.Query<_JobQueue>()
                    .Where(i => i.Job.Id != long.Parse(payload.JobId))
                    .Select(i => i.FetchedAt)
                    .Single();

                Assert.Null(otherJobFetchedAt);
            });
        }

        [Fact]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsEmpty()
        {
            _storage.UseStatelessSession(session =>
            {
                var queue = CreateJobQueue(_storage);

                var exception = Assert.Throws<ArgumentException>(
                    () => queue.Dequeue(new string[0], CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsNull()
        {
            _storage.UseStatelessSession(session =>
            {
                var queue = CreateJobQueue(_storage);

                var exception = Assert.Throws<ArgumentNullException>(
                    () => queue.Dequeue(null, CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact]
        public void Dequeue_ShouldWaitIndefinitely_WhenThereAreNoJobs()
        {
            _storage.UseStatelessSession(session =>
            {
                var cts = new CancellationTokenSource(200);
                var queue = CreateJobQueue(_storage);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            });
        }

        [Fact]
        public void Dequeue_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning()
        {
            _storage.UseStatelessSession(session =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                var queue = CreateJobQueue(_storage);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            });
        }

        [Fact]
        public void Enqueue_AddsAJobToTheQueue()
        {
            _storage.UseStatelessSession(session =>
            {
                session.DeleteAll<_JobQueue>();
                //does nothing

                var newJob = JobInsertionHelper.InsertNewJob(session);

                var queue = CreateJobQueue(_storage);

                queue.Enqueue(session, "default", newJob.Id.ToString());

                var record = session.Query<_JobQueue>().Single();
                Assert.Equal(newJob.Id, record.Job.Id);
                Assert.Equal("default", record.Queue);
                Assert.Null(record.FetchedAt);
            });
        }
    }
}