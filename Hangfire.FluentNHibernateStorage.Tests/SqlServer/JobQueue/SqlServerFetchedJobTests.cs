using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.JobQueue
{
    [Collection(Constants.SqlServerFixtureCollectionName)]
    public class
        SqlServerFetchedJobTests : FetchedJobTestsBase
    {
        public SqlServerFetchedJobTests(SqlServerTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}