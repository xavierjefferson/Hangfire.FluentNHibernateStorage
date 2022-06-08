using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.JobQueue
{
    [Xunit.Collection(Constants.SqliteFixtureCollectionName)]
    public class
        SqliteFluentNHibernateJobQueueMonitoringApiTests : FluentNHibernateJobQueueMonitoringApiTests
    {
        public SqliteFluentNHibernateJobQueueMonitoringApiTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}