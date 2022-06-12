using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.MySql.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.MySql.JobQueue
{
    [Xunit.Collection(Constants.MySqlFixtureCollectionName)]
    public class
        MySqlJobQueueMonitoringApiTests : JobQueueMonitoringApiTests
    {
        public MySqlJobQueueMonitoringApiTests(MySqlTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}