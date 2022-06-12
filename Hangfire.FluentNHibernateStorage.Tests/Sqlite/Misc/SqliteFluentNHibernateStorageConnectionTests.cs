using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Sqlite.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Misc
{
    [Collection(Constants.SqliteFixtureCollectionName)]
    public class
        SqliteFluentNHibernateStorageConnectionTests : FluentNHibernateStorageConnectionTestsBase
    {
        public SqliteFluentNHibernateStorageConnectionTests(SqliteTestDatabaseFixture fixture, ITestOutputHelper testOutputHelper) : base(fixture, testOutputHelper)
        {
        }
    }
}