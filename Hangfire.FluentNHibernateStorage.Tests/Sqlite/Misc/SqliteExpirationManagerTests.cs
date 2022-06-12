using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Sqlite.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Misc
{
    [Collection(Constants.SqliteFixtureCollectionName)]
    public class SqliteExpirationManagerTests : ExpirationManagerTestsBase
    {
        public SqliteExpirationManagerTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}