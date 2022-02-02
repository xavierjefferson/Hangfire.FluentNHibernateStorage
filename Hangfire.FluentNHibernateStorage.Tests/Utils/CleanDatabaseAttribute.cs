using System.Reflection;
using System.Transactions;
using Xunit.Sdk;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class CleanDatabaseAttribute : BeforeAfterTestAttribute
    {
        private readonly IsolationLevel _isolationLevel;

        

        public CleanDatabaseAttribute(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _isolationLevel = isolationLevel;
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            new FluentNHibernateJobStorageTestWrapper(ConnectionUtils.GetStorage()).TruncateAllHangfireTables();
            
        }

        public override void After(MethodInfo methodUnderTest)
        {
        }
    }
}