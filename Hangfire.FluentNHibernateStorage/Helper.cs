using System;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class Helper
    {
        public  const string ValueParameterName = "value";
        public const string IdParameterName = "id"; 

        internal static string GetSingleFieldUpdateSql(string table, string column, string idcolumn)
        {
            return string.Format("update {0} set {1}=:{3} where {2}=:{4}", table, column, idcolumn, ValueParameterName,IdParameterName);
        }
        public static void UpsertEntity<T>(this IStatelessSession session, Expression<Func<T, bool>> matchFunc, Action<T> changeFunc,
            Action<T> keysetAction) where T : new()
        {
            var entity = session.Query<T>().FirstOrDefault(matchFunc);
            if (entity == null)
            {
                entity = new T();
                keysetAction(entity);
                changeFunc(entity);
                session.Insert(entity);
            }
            else
            {
                changeFunc(entity);
                session.Update(entity);
            }
        }

        public static void UpsertEntity<T>(this ISession session, Expression<Func<T, bool>> matchFunc, Action<T> changeFunc,
            Action<T> keysetAction) where T : new()
        {
            var entity = session.Query<T>().FirstOrDefault(matchFunc);
            if (entity == null)
            {
                entity = new T();
                keysetAction(entity);
            }
            changeFunc(entity);
            session.Save(entity);
            session.Flush();
        }
    }
}