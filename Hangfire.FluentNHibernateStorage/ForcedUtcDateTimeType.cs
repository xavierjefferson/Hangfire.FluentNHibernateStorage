using System;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using NHibernate.Engine;
using NHibernate.Type;

namespace Hangfire.FluentNHibernateStorage
{
    /// <summary>
    ///     This type save the <see cref="DateTime" /> to the database. You need to save the <see cref="DateTime" /> in UTC (
    ///     <see cref="DateTimeKind.Utc" />).
    ///     When you load the <see cref="DateTime" />, then time is in UTC.
    /// </summary>
    /// <seealso cref="http://stackoverflow.com/questions/29352719/save-and-load-utc-datetime-with-nhibernate" />
    public class ForcedUtcDateTimeType : DateTimeType
    {
        private static readonly string _name = Regex.Replace(nameof(ForcedUtcDateTimeType), "Type$", "");
        public override string Name => _name;

        /// <summary>
        ///     Sets the value of this Type in the IDbCommand.
        /// </summary>
        /// <param name="st">The IDbCommand to add the Type's value to.</param>
        /// <param name="value">The value of the Type.</param>
        /// <param name="index">The index of the IDataParameter in the IDbCommand.</param>
        /// <remarks>
        ///     No null values will be written to the IDbCommand for this Type.
        ///     The <see cref="DateTime.Kind" /> must be <see cref="DateTimeKind.Utc" />.
        /// </remarks>
        public override void Set(DbCommand st, object value, int index, ISessionImplementor session)
        {
            if (value == null)
            {
                base.Set(st, value, index, session);
                return;
            }

            DateTime toWrite;
            var dateTime = (DateTime) (value is DateTime ? value : DateTime.UtcNow);
            switch (dateTime.Kind)
            {
                case DateTimeKind.Utc:
                    //leave as is
                    toWrite = dateTime;
                    break;
                case DateTimeKind.Local:
                    //convert to universal first
                    toWrite = dateTime.ToUniversalTime();
                    break;
                case DateTimeKind.Unspecified:
                    //assume local then convert to utc
                    toWrite = DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
                    break;
                default:
                    throw new Exception($"Unknown {nameof(DateTime)}.{nameof(DateTime.Kind)} '{dateTime.Kind}'");
            }

            // Change the kind to unspecified, because when we load the datetime we have wrong values with kind utc.
            ((IDataParameter) st.Parameters[index]).Value = DateTime.SpecifyKind(toWrite, DateTimeKind.Unspecified);
        }

        public override object Get(DbDataReader rs, int index, ISessionImplementor session)
        {
            return ChangeDateTimeKindToUtc(base.Get(rs, index, session));
        }

        public override object Get(DbDataReader rs, string name, ISessionImplementor session)
        {
            return ChangeDateTimeKindToUtc(base.Get(rs, name, session));
        }

        [Obsolete]
        public override object FromStringValue(string xml)
        {
            return ChangeDateTimeKindToUtc(base.FromStringValue(xml));
        }

        private object ChangeDateTimeKindToUtc(object value)
        {
            if (value == null) return null;
            var dateTime = (DateTime) value;
            return dateTime.Kind == DateTimeKind.Utc ? dateTime : new DateTime(dateTime.Ticks, DateTimeKind.Utc);
        }
    }
}