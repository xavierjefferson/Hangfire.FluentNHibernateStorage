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
                    queryString = string.Format("delete from {0} where {1} in (:{2})", typeName.Name,
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