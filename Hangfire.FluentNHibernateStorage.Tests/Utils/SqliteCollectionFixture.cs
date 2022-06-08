using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    [CollectionDefinition(Constants.SqliteFixtureCollectionName)]
    public class SqliteCollectionFixture : ICollectionFixture<SqliteTestDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}