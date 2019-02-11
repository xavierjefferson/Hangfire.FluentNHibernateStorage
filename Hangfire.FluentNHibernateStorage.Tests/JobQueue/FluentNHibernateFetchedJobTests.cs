using System;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.JobQueue
{
    public class FluentNHibernateFetchedJobTests : IClassFixture<TestDatabaseFixture>
    {
        public FluentNHibernateFetchedJobTests()
        {
            _fetchedJob = new FetchedJob {Id = _id, JobId = JobId, Queue = Queue};

            var options = new FluentNHibernateStorageOptions {PrepareSchemaIfNecessary = false};
            _storage = new Mock<FluentNHibernateJobStorage>(ConnectionUtils.GetPersistenceConfigurer(), options);
        }

        private const int JobId = 1;
        private const string Queue = "queue";


        private readonly FetchedJob _fetchedJob;
        private readonly int _id = 0;
        private readonly Mock<FluentNHibernateJobStorage> _storage;

        private FluentNHibernateFetchedJob CreateFetchedJob(int jobId, string queue)
        {
            return new FluentNHibernateFetchedJob(_storage.Object,
                new FetchedJob {JobId = jobId, Queue = queue, Id = _id});
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var fetchedJob = new FluentNHibernateFetchedJob(_storage.Object, _fetchedJob);

            Assert.Equal(JobId.ToString(), fetchedJob.JobId);
            Assert.Equal(Queue, fetchedJob.Queue);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateFetchedJob(null, _fetchedJob));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateFetchedJob(null, _fetchedJob));

            Assert.Equal("storage", exception.ParamName);
        }
    }
}