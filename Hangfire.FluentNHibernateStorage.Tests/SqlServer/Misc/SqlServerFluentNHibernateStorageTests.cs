using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.Misc
{
    [Xunit.Collection(Constants.SqlServerFixtureCollectionName)]
    public class SqlServerFluentNHibernateStorageTests : FluentNHibernateStorageTests
    {
        public SqlServerFluentNHibernateStorageTests(SqlServerTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}