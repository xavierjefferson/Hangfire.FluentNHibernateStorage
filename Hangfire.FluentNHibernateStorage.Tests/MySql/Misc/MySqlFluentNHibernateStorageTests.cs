using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.MySql.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.MySql.Misc
{
    [Xunit.Collection(Constants.MySqlFixtureCollectionName)]
    public class MySqlFluentNHibernateStorageTests : FluentNHibernateStorageTests
    {
        public MySqlFluentNHibernateStorageTests(MySqlTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}