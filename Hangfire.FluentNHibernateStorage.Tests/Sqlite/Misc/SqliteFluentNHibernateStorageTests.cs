using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Sqlite.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Misc
{
    [Collection(Constants.SqliteFixtureCollectionName)]
    public class SqliteFluentNHibernateStorageTests : FluentNHibernateStorageTests
    {
        public SqliteFluentNHibernateStorageTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}