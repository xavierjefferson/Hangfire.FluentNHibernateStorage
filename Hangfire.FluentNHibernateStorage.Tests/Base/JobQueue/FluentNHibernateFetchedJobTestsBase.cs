using System;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue
{
    public abstract class FluentNHibernateFetchedJobTestsBase : TestBase
    {
        protected FluentNHibernateFetchedJobTestsBase(TestDatabaseFixture fixture) : base(fixture)
        {
            _fetchedJob = new FetchedJob {Id = _id, JobId = JobId, Queue = Queue};


            _storageMock = CreateMock(GetPersistenceConfigurer());
        }

        private const int JobId = 1;
        private const string Queue = "queue";


        private readonly FetchedJob _fetchedJob;
        private readonly int _id = 0;
        private readonly Mock<FluentNHibernateJobStorage> _storageMock;

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var fetchedJob = new FluentNHibernateFetchedJob(_storageMock.Object, _fetchedJob);

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