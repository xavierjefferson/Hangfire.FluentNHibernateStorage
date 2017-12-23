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
            //if (_isolationLevel != IsolationLevel.Unspecified)
            //{
            //    _transaction = new TransactionScope(
            //        TransactionScopeOption.RequiresNew,
            //        new TransactionOptions {IsolationLevel = _isolationLevel});
            //}
        }

        public override void After(MethodInfo methodUnderTest)
        {
            //try
            //{
            //    if (_transaction != null)
            //    {
            //        _transaction.Dispose();
            //    }
            //}
            //finally
            //{
            //}
        }
    }
}