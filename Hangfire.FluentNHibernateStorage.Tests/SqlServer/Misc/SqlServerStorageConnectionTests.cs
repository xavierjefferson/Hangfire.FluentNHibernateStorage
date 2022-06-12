using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures;
using Xunit.Abstractions;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.Misc
{
    [Xunit.Collection(Constants.SqlServerFixtureCollectionName)]
    public class
        SqlServerStorageConnectionTests : StorageConnectionTestsBase
    {
        public SqlServerStorageConnectionTests(SqlServerTestDatabaseFixture fixture, ITestOutputHelper testOutputHelper) : base(fixture, testOutputHelper)
        {
        }
    }
}