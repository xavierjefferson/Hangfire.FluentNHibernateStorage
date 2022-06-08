using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Misc
{
    [Collection(Constants.SqliteFixtureCollectionName)]
    public class
        SqliteFluentNHibernateWriteOnlyTransactionTests : FluentNHibernateWriteOnlyTransactionTestsBase
    {
        public SqliteFluentNHibernateWriteOnlyTransactionTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}