using System;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class ArgumentHelper
    {
        public static void ThrowIfValueIsNotPositive(TimeSpan value, string fieldName)
        {
            var message = $"The {fieldName} property value should be positive. Given: {value}.";

            if (value == TimeSpan.Zero || value != value.Duration())
                throw new ArgumentException(message, nameof(value));
        }

        public static void ThrowIfValueIsNotPositive(int? value, string fieldName)
        {
            var message = $"The {fieldName} property value should be positive. Given: {value}.";

            if (value.HasValue && value.Value <= 0)
                throw new ArgumentException(message, nameof(value));
        }
    }
}