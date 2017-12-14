using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Hangfire.FluentNHibernateStorage.Entities;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class Helper2
    {
        private static readonly Dictionary<Type, string> DeleteCommands = new Dictionary<Type, string>();
        private static readonly object mutex = new object();

        public static void DeleteById<T>(this ISession session, int id) where T : IExpireWithId
        {
            DeleteById<T>(session, new[] {id});
        }

        public static long DeleteById<T>(this ISession session, ICollection<int> id) where T : IExpireWithId
        {
            if (!id.Any())
            {
                return 0;
            }
            string queryString;
            lock (mutex)
            {
                if (DeleteCommands.ContainsKey(typeof(T)))
                {
                    queryString = DeleteCommands[typeof(T)];
                }
                else
                {
                    queryString = string.Format("delete from {0} where {1} in (:{2})", nameof(T),
                        nameof(IExpireWithId.Id), Helper.IdParameterName);
                    DeleteCommands[typeof(T)] = queryString;
                }
            }
            return session.CreateQuery(queryString).SetParameterList(Helper.IdParameterName, id).ExecuteUpdate();
        }

        public static void DoActionByExpression<T>(this ISession session, Expression<Func<T, bool>> expr,
            Action<T> action)
            where T : IExpireWithId
        {
            var entities = session.Query<T>().Where(expr);
            foreach (var entity in entities)
            {
                action(entity);
            }
        }

        public static void DeleteByExpression<T>(this ISession session, int id, Expression<Func<T, bool>> expr)
            where T : IExpireWithId
        {
            var idList = session.Query<T>().Where(expr).Select(i => i.Id).ToList();

            DeleteById<T>(session, idList);
        }
    }
}