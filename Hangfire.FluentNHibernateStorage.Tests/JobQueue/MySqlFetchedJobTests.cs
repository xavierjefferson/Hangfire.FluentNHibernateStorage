using System;

namespace Hangfire.FluentNHibernateStorage.Tests.JobQueue
{
    public class MySqlFetchedJobTests : IClassFixture<TestDatabaseFixture>
    {
        private const int JobId = 1;
        private const string Queue = "queue";

        private readonly Mock<IDbConnection> _connection;
        private readonly FetchedJob _fetchedJob;
        private readonly int _id = 0;
        private readonly Mock<NHStorage> _storage;

        public MySqlFetchedJobTests()
        {
            _fetchedJob = new FetchedJob {Id = _id, JobId = JobId, Queue = Queue};
            _connection = new Mock<IDbConnection>();
            var options = new NHStorageOptions {PrepareSchemaIfNecessary = false};
            _storage = new Mock<NHStorage>(ConnectionUtils.GetConnectionString(), options);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MySqlFetchedJob(null, _connection.Object, _fetchedJob));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MySqlFetchedJob(_storage.Object, null, _fetchedJob));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var fetchedJob = new MySqlFetchedJob(_storage.Object, _connection.Object, _fetchedJob);

            Assert.Equal(JobId.ToString(), fetchedJob.JobId);
            Assert.Equal(Queue, fetchedJob.Queue);
        }

        private MySqlFetchedJob CreateFetchedJob(int jobId, string queue)
        {
            return new MySqlFetchedJob(_storage.Object, _connection.Object,
                new FetchedJob {JobId = jobId, Queue = queue, Id = _id});
        }
    }
}