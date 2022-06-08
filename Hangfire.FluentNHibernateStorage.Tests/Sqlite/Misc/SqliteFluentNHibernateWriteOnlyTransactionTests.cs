using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Providers;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Misc
{
    public class
        SqliteFluentNHibernateWriteOnlyTransactionTests : FluentNHibernateWriteOnlyTransactionTestsBase<SqliteProvider, SqliteDatabaseFixture>
    {
    }
}