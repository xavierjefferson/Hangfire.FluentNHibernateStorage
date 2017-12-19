using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Maps;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class SQLHelper
    {
        public const string ValueParameterName = "newValue";
        public const string ValueParameter2Name = "newValue2";
        public const string IdParameterName = "entityId";

        private static readonly Dictionary<Type, string> DeleteByIdCommands = new Dictionary<Type, string>();
        private static readonly object mutex = new object();

        internal static readonly string UpdateJobParameterValueStatement =
            GetSingleFieldUpdateSql(nameof(_JobParameter), nameof(_JobParameter.Value),
                nameof(_JobParameter.Id));

        internal static readonly string DeleteServerByNameStatement =
            string.Format("delete from {0} where {1}=:{2}", nameof(_Server).WrapObjectName(),
                nameof(_Server.Id).WrapObjectName(),
                IdParameterName);

        internal static readonly string UpdateServerLastHeartbeatStatement =
            GetSingleFieldUpdateSql(nameof(_Server), nameof(_Server.LastHeartbeat), nameof(_Server.Id));

        internal static readonly string DeleteServerByLastHeartbeatStatement = string.Format(
            "delete from {0} where {1} < :{2}",
            nameof(_Server).WrapObjectName(),
            nameof(_Server.LastHeartbeat).WrapObjectName(), ValueParameterName);

       
        internal static readonly string UpdateJobExpireAtStatement =
            GetSingleFieldUpdateSql(nameof(_Job), nameof(_Job.ExpireAt), nameof(_Job.Id));

        internal static readonly string UpdateJobQueueFetchedAtStatement =
            GetSingleFieldUpdateSql(nameof(_JobQueue), nameof(_JobQueue.FetchedAt), nameof(_JobQueue.Id));

        internal static readonly string DeleteJobQueueStatement = string.Format("delete from {0} where {1}=:{2}",
            nameof(_JobQueue).WrapObjectName(),
            nameof(_JobQueue.Id).WrapObjectName(), IdParameterName);

        internal static readonly string UpdateAggregateCounterSql = string.Format(
            "update {0} s set s.{1}=s.{1} + :{4}, s.{3}= case when s.{3} >  :{6} then s.{3} else :{6} end where s.{2} = :{5}",
            nameof(_AggregatedCounter).WrapObjectName(), nameof(_AggregatedCounter.Value).WrapObjectName(),
            nameof(_AggregatedCounter.Key).WrapObjectName(),
            nameof(_AggregatedCounter.ExpireAt).WrapObjectName(), ValueParameterName, IdParameterName,
            ValueParameter2Name);

        internal static readonly Dictionary<Type, string> DeleteByKeyStatementDictionary = new Dictionary<Type, string>
        {
            {typeof(_Set), GetDeleteByKeyStatement<_Set>()},
            {typeof(_Hash), GetDeleteByKeyStatement<_List>()},
            {typeof(_List), GetDeleteByKeyStatement<_List>()}
        };

        internal static readonly Dictionary<Type, string> DeleteByKeyValueStatementlDictionary =
            new Dictionary<Type, string>
            {
                {typeof(_Set), GetDeleteByKeyValueStatement<_Set>()},
                {typeof(_Hash), GetDeleteByKeyValueStatement<_List>()},
                {typeof(_List), GetDeleteByKeyValueStatement<_List>()}
            };

        internal static readonly Dictionary<Type, string> SetExpireStatementDictionary = new Dictionary<Type, string>
        {
            {typeof(_Set), GetSetExpireByKeyStatement<_Set>()},
            {typeof(_Hash), GetSetExpireByKeyStatement<_List>()},
            {typeof(_List), GetSetExpireByKeyStatement<_List>()}
        };

        internal static readonly string DeleteDistributedLockSql = string.Format("delete from {0} where {1}=:{2}",
            nameof(_DistributedLock).WrapObjectName(),
            nameof(_DistributedLock.Resource).WrapObjectName(), IdParameterName);

        private static string GetSingleFieldUpdateSql(string table, string column, string idcolumn)
        {
            return string.Format("update {0} set {1}=:{3} where {2}=:{4}", table.WrapObjectName(),
                column.WrapObjectName(), idcolumn.WrapObjectName(),
                ValueParameterName,
                IdParameterName);
        }

        public static void UpsertEntity<T>(this IWrappedSession session, Expression<Func<T, bool>> matchFunc,
            Action<T> changeFunc,
            Action<T> keysetAction) where T : new()
        {
            var entity = session.Query<T>().SingleOrDefault(matchFunc);
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
                if (DeleteByIdCommands.ContainsKey(typeName))
                {
                    queryString = DeleteByIdCommands[typeName];
                }
                else
                {
                    queryString = string.Format("delete from {0} where {1} in (:{2})", typeName.Name.WrapObjectName(),
                        nameof(IInt32Id.Id).WrapObjectName(), IdParameterName);
                    DeleteByIdCommands[typeName] = queryString;
                }
            }
            return session.CreateQuery(queryString).SetParameterList(IdParameterName, id).ExecuteUpdate();
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

        internal static string GetSetExpireByKeyStatement<T>() where T : IExpirableWithKey
        {
            return string.Format("update {0} set {1}={2} where {3}:={4}", typeof(T).Name.WrapObjectName(),
                nameof(IExpirable.ExpireAt).WrapObjectName(), ValueParameterName,
                nameof(IExpirableWithKey.Key).WrapObjectName(),
                IdParameterName);
        }

        internal static string GetDeleteByKeyStatement<T>() where T : IExpirableWithKey
        {
            return string.Format("delete from {0} where {1}:={2}", typeof(T).Name.WrapObjectName(),
                nameof(IExpirableWithKey.Key).WrapObjectName(),
                ValueParameterName);
        }

        internal static string GetDeleteByKeyValueStatement<T>() where T : IKeyWithStringValue
        {
            return string.Format("delete from {0} where {1}:={2} and {3}=:{4}", typeof(T).Name.WrapObjectName(),
                nameof(IExpirableWithKey.Key).WrapObjectName(),
                ValueParameterName, nameof(IKeyWithStringValue.Value).WrapObjectName(), ValueParameter2Name);
        }
    }
}