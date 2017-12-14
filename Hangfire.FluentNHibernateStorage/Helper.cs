using System;
using System.Linq;
using System.Linq.Expressions;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class Helper
    {
        public const string ValueParameterName = "newValue";
        public const string ValueParameter2Name = "newValue2";
        public const string IdParameterName = "entityId";

        internal static string GetSingleFieldUpdateSql(string table, string column, string idcolumn)
        {
            return string.Format("update {0} set {1}=:{3} where {2}=:{4}", table, column, idcolumn, ValueParameterName,
                IdParameterName);
        }

        public static void UpsertEntity<T>(this IWrappedSession session, Expression<Func<T, bool>> matchFunc,
            Action<T> changeFunc,
            Action<T> keysetAction) where T : new()
        {
            using (var p = session.BeginTransaction())
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
                session.Flush();
                p.Commit();
            }
        }
    }
}