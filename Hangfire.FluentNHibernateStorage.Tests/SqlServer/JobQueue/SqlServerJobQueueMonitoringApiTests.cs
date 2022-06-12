using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.JobQueue
{
    [Xunit.Collection(Constants.SqlServerFixtureCollectionName)]
    public class
        SqlServerJobQueueMonitoringApiTests : JobQueueMonitoringApiTests
    {
        public SqlServerJobQueueMonitoringApiTests(SqlServerTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}