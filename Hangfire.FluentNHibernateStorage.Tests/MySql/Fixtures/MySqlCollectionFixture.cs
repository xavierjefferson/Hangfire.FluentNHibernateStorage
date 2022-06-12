using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.MySql.Fixtures
{
    [CollectionDefinition(Constants.MySqlFixtureCollectionName)]
    public class MySqlCollectionFixture : ICollectionFixture<MySqlTestDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}