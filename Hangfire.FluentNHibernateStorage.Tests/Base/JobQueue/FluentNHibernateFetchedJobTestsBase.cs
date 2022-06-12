using System;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue
{
    public abstract class FluentNHibernateFetchedJobTestsBase : TestBase
    {
        protected FluentNHibernateFetchedJobTestsBase(DatabaseFixtureBase fixture) : base(fixture)
        {
            _fetchedJob = new FetchedJob {Id = _id, JobId = JobId, Queue = Queue};
        }

        private const int JobId = 1;
        private const string Queue = "queue";


        private readonly FetchedJob _fetchedJob;
        private readonly int _id = 0;
        private Mock<FluentNHibernateJobStorage> _storageMock;

        public override FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null)
        {
            if (_storageMock == null) _storageMock = GetStorageMock();
           var tmp =  _storageMock.Object;
           return tmp;
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var fetchedJob = new FluentNHibernateFetchedJob(GetStorage(), _fetchedJob);

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