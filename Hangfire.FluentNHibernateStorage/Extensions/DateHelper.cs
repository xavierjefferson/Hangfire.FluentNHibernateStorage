using System;

namespace Hangfire.FluentNHibernateStorage.Extensions
{
    public static class DateHelper
    {
        private static readonly DateTime Horizon = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

        public static long ToEpochDate(this DateTime dateTime)
        {
            return Convert.ToInt64(dateTime.Subtract(Horizon).TotalMilliseconds);
        }

        public static DateTime FromEpochDate(this long dateTime)
        {
            return Horizon.AddMilliseconds(dateTime);
        }
    }
}