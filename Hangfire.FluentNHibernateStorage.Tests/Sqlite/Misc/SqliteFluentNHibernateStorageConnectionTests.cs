using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Misc
{
    [Collection(Constants.SqliteFixtureCollectionName)]
    public class
        SqliteFluentNHibernateStorageConnectionTests : FluentNHibernateStorageConnectionTestsBase
    {
        public SqliteFluentNHibernateStorageConnectionTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}