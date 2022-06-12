using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.JobQueue
{
    [Xunit.Collection(Constants.SqlServerFixtureCollectionName)]
    public class
        SqlServerJobQueueTests : JobQueueTestsBase
    {
        public SqlServerJobQueueTests(SqlServerTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}