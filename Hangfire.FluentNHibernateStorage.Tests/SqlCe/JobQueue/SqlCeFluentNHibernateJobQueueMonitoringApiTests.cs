using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Providers;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.JobQueue
{
    
    public class
        SqlCeFluentNHibernateJobQueueMonitoringApiTests : FluentNHibernateJobQueueMonitoringApiTests<SqlCeProvider, SqlCeDatabaseFixture>
    {
    }
}