using System;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class Helper
    {
        internal static string singlefieldupdate(string table, string column, string idcolumn)
        {
            return string.Format("update {0} set {1}=:value where {2}=:id", table, column, idcolumn);
        }

        public static void Upsert<T>(this ISession x, Expression<Func<T, bool>> matchFunc, Action<T> changeFunc,
            Action<T> keysetAction) where T : new()
        {
            var p = x.Query<T>().FirstOrDefault(matchFunc);
            if (p == null)
            {
                var x1 = new T();
                changeFunc(x1);
                keysetAction(x1);
                x.Save(x1);
                x.Flush();
            }
        }
    }
}