using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Sqlite.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.JobQueue
{
    [Xunit.Collection(Constants.SqliteFixtureCollectionName)]
    public class SqliteFetchedJobTests : FetchedJobTestsBase
    {
        public SqliteFetchedJobTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}