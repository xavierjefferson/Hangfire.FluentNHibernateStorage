using System;
using System.Diagnostics;
using System.Threading;
using Hangfire.Common;

namespace Hangfire.FluentNHibernateStorage.Extensions
{
    public static class CancellationTokenExtensions
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Do a wait but check often to see if cancellation has been requested for smoother shutdown
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="interval"></param>
        public static void PollForCancellation(this CancellationToken cancellationToken, TimeSpan interval)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.Elapsed < interval)
            {
                cancellationToken.Wait(PollInterval);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}