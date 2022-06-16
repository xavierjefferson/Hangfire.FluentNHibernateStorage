using System;

namespace Hangfire.FluentNHibernateStorage.Extensions
{
    public static class DateHelper
    {
        public static long ToEpochDate(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        public static DateTime FromEpochDate(this long dateTime)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(dateTime).UtcDateTime;
        }
    }
}