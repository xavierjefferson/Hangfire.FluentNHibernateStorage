using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.MySql.Fixtures;
using Xunit.Abstractions;

namespace Hangfire.FluentNHibernateStorage.Tests.MySql.Misc
{
    [Xunit.Collection(Constants.MySqlFixtureCollectionName)]
    public class
        MySqlStorageConnectionTests : StorageConnectionTestsBase
    {
        public MySqlStorageConnectionTests(MySqlTestDatabaseFixture fixture, ITestOutputHelper testOutputHelper) : base(fixture, testOutputHelper)
        {
        }
    }
}