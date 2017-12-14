using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Hangfire.FluentNHibernateStorage.Entities;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class z0
    {
        private static readonly Dictionary<Type, string> dz = new Dictionary<Type, string>();
        private static readonly object mutex = new object();

        public static void DeleteById<T>(this ISession session, int id) where T : IExpireWithId
        {
            var sql = GetDelByIds<T>();
            session.CreateQuery(sql).SetParameterList("id", new[] {id});
        }

        public static void UpdateByExpression<T>(this ISession session, Expression<Func<T, bool>> expr, Action<T> ac)
            where T : IExpireWithId
        {
            var z = session.Query<T>().Where(expr);
            foreach (var x in z)
            {
                ac(x);
            }
        }

        public static void DeleteByExpression<T>(this ISession session, int id, Expression<Func<T, bool>> expr)
            where T : IExpireWithId
        {
            var z = session.Query<T>().Where(expr).Select(i => i.Id).ToList();
            if (z.Any())
            {
                var sql = GetDelByIds<T>();

                session.CreateQuery(sql).SetParameterList("id", z);
            }
        }

        private static string GetDelByIds<T>() where T : IExpireWithId
        {
            string sql;
            lock (mutex)
            {
                if (dz.ContainsKey(typeof(T)))
                {
                    sql = dz[typeof(T)];
                }
                else
                {
                    sql = string.Format("delete from {0} where {1} in (:id)", nameof(T), nameof(IExpireWithId.Id));
                    dz[typeof(T)] = sql;
                }
            }
            return sql;
        }
    }
}