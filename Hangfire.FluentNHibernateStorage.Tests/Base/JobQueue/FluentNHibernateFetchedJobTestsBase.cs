using System;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Providers;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue
{
    public abstract class FluentNHibernateFetchedJobTestsBase<T, U> : TestBase<T, U> where T : IDbProvider, new() where U:TestDatabaseFixture
    {
        protected FluentNHibernateFetchedJobTestsBase()
        {
            _fetchedJob = new FetchedJob {Id = _id, JobId = JobId, Queue = Queue};

           
            _storage = CreateMock(GetPersistenceConfigurer() );
        }

        private const int JobId = 1;
        private const string Queue = "queue";


        private readonly FetchedJob _fetchedJob;
        private readonly int _id = 0;
        private readonly Mock<FluentNHibernateJobStorage> _storage;

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