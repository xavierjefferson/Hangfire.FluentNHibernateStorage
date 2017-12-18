using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class Helper
    {
        public const string ValueParameterName = "newValue";
        public const string ValueParameter2Name = "newValue2";
        public const string IdParameterName = "entityId";

        internal static string GetSingleFieldUpdateSql(string table, string column, string idcolumn)
        {
            return String.Format("update `{0}` set `{1}`=:{3} where `{2}`=:{4}", table, column, idcolumn,
                ValueParameterName,
                IdParameterName);
        }

        public static void UpsertEntity<T>(this IWrappedSession session, Expression<Func<T, bool>> matchFunc,
            Action<T> changeFunc,
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
            session.Flush();
        }

        private static readonly Dictionary<Type, string> DeleteCommands = new Dictionary<Type, string>();
        private static readonly object mutex = new object();

        public static long DeleteByInt32Id<T>(this IWrappedSession session, ICollection<int> id) where T : IInt32Id
        {
            if (!id.Any())
            {
                return 0;
            }
            string queryString;
            lock (mutex)
            {
                var typeName = typeof(T);
                if (DeleteCommands.ContainsKey(typeName))
                {
                    queryString = DeleteCommands[typeName];
                }
                else
                {
                    queryString = String.Format("delete from {0} where {1} in (:{2})", typeName.Name,
                        nameof(IInt32Id.Id), Helper.IdParameterName);
                    DeleteCommands[typeName] = queryString;
                }
            }
            return session.CreateQuery(queryString).SetParameterList(Helper.IdParameterName, id).ExecuteUpdate();
        }

        public static void DoActionByExpression<T>(this IWrappedSession session, Expression<Func<T, bool>> expr,
            Action<T> action)
            where T : IExpirableWithId
        {
            var entities = session.Query<T>().Where(expr);
            foreach (var entity in entities)
            {
                action(entity);
            }
        }
    }
}