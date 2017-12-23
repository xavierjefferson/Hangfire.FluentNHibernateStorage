using System.Reflection;
using System.Transactions;
using Xunit.Sdk;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class CleanDatabaseAttribute : BeforeAfterTestAttribute
    {
        private readonly IsolationLevel _isolationLevel;

        private TransactionScope _transaction;

        public CleanDatabaseAttribute(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _isolationLevel = isolationLevel;
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            ConnectionUtils.GetStorage().ResetAll();

        }

        public override void After(MethodInfo methodUnderTest)
        {
            
        }
    }
}