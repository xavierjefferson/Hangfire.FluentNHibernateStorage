using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Misc
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeFluentNHibernateWriteOnlyTransactionTests : FluentNHibernateWriteOnlyTransactionTestsBase
    {
        public SqlCeFluentNHibernateWriteOnlyTransactionTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}