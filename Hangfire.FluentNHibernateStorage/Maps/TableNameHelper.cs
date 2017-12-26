using System;
using System.Linq;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class TableNameHelper
    {
        public static string GetTableName(Type z)
        {
            var a = z.GetCustomAttributes(false).Cast<TableNameRootAttribute>().FirstOrDefault();
            if (a != null)
            {
                return a.Name;
            }
            return null;
        }
    }
}