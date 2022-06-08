using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.JobQueue
{
    [Xunit.Collection(Constants.SqliteFixtureCollectionName)]
    public class SqliteFluentNHibernateJobQueueTests : FluentNHibernateJobQueueTestsBase
    {
        public SqliteFluentNHibernateJobQueueTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}