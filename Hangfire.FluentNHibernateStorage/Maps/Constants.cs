namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class Constants
    {
        public const int VarcharMaxLength = 4001;

        public class ColumnNames
        {
            public static readonly string JobId = "JobId";
            public static readonly string Id = "Id";
            public static readonly string Data = "Data";
            public static readonly string CreatedAt = "CreatedAt";
        }

        public const int StateReasonLength = 100;
        public const int StateDataLength = Constants.VarcharMaxLength;
        public const int StateNameLength = 20;
    }
}