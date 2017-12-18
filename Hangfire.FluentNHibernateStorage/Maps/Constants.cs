namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class Constants
    {
        public const int VarcharMaxLength = 4001;
        public static readonly string JobId = "JobId".WrapObjectName();
        public static readonly string Id = "Id".WrapObjectName();
        public static readonly string Data = "Data".WrapObjectName();
        public static readonly string CreatedAt = "CreatedAt".WrapObjectName();
    }
}