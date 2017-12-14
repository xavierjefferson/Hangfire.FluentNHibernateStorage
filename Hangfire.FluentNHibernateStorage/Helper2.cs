using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class Helper2
    {
        private static readonly Dictionary<Type, string> DeleteCommands = new Dictionary<Type, string>();
        private static readonly object mutex = new object();

        public static void DeleteById<T>(this IWrappedSession session, int id) where T : IExpireWithId
        {
            DeleteById<T>(session, new[] {id});
        }

        public static long DeleteById<T>(this IWrappedSession session, ICollection<int> id) where T : IExpireWithId
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
                    queryString = string.Format("delete from {0} where {1} in (:{2})", typeName.Name,
                        nameof(IExpireWithId.Id), Helper.IdParameterName);
                    DeleteCommands[typeName] = queryString;
                }
            }
            return session.CreateQuery(queryString).SetParameterList(Helper.IdParameterName, id).ExecuteUpdate();
        }


        public static void DoActionByExpression<T>(this IWrappedSession session, Expression<Func<T, bool>> expr,
            Action<T> action)
            where T : IExpireWithId
        {
            var entities = session.Query<T>().Where(expr);
            foreach (var entity in entities)
            {
                action(entity);
            }
        }

        public static void DeleteByExpression<T>(this IWrappedSession session, int id, Expression<Func<T, bool>> expr)
            where T : IExpireWithId
        {
            var idList = session.Query<T>().Where(expr).Select(i => i.Id).ToList();

            DeleteById<T>(session, idList);
        }
    }
}