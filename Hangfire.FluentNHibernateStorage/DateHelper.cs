using System;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class DateHelper
    {
        private static readonly DateTime horizon = new DateTime(1970, 1, 1);

        public static long ToUnixDate(this DateTime dt)
        {
            return Convert.ToInt64(dt.Subtract(horizon).TotalMilliseconds);
        }
    }
}