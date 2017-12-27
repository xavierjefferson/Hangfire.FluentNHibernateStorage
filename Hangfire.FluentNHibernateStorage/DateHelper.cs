using System;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class DateHelper
    {
        public static readonly DateTime Horizon = new DateTime(1970, 1, 1);

        public static long ToUnixDate(this DateTime dt)
        {
            return Convert.ToInt64(dt.Subtract(Horizon).TotalMilliseconds);
        }
    }
}