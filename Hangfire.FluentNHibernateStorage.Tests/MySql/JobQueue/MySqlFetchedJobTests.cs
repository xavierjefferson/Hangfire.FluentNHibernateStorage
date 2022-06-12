using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.MySql.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.MySql.JobQueue
{
    [Collection(Constants.MySqlFixtureCollectionName)]
    public class
        MySqlFetchedJobTests : FetchedJobTestsBase
    {
        public MySqlFetchedJobTests(MySqlTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}