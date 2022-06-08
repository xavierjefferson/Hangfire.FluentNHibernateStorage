using System;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    internal static class JobInsertionHelper
    {
        public static _Job InsertNewJob(StatelessSessionWrapper session, Action<_Job> action = null)
        {
            var newJob = new _Job
            {
                InvocationData = string.Empty,
                Arguments = string.Empty,
                CreatedAt = session.Storage.UtcNow
            };
            action?.Invoke(newJob);
            session.Insert(newJob);

            return newJob;
        }
    }
}