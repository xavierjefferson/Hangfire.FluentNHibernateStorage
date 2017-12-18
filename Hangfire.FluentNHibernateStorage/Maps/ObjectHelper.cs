namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal static class ObjectHelper
    {
        public static string WrapObjectName(this string x)
        {
            return string.Format("`{0}`", x);
        }
    }
}