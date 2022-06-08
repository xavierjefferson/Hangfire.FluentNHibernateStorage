using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.JobQueue
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeFluentNHibernateJobQueueMonitoringApiTests : FluentNHibernateJobQueueMonitoringApiTests
    {
        public SqlCeFluentNHibernateJobQueueMonitoringApiTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}