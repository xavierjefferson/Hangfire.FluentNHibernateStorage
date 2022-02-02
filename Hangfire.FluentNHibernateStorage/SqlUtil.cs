using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Maps;
using Hangfire.Logging;
using NHibernate;
using NHibernate.Exceptions;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    internal static class SqlUtil
    {
        public const string ValueParameterName = "newValue";
        public const string ValueParameter2Name = "newValue2";
        public const string IdParameterName = "entityId";

        private const int DeleteBatchSize = 250;
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(SqlUtil));


        /// <summary>
        ///     HQL statement by which a Server entity will have its LastHeartBeat property updated
        /// </summary>
        internal static readonly string UpdateServerLastHeartbeatStatement =
            GetSingleFieldUpdateSql(nameof(_Server), nameof(_Server.LastHeartbeat), nameof(_Server.Id));


        /// <summary>
        ///     HQL statement by which the ExpireAt property of a Job entity will be updated
        /// </summary>
        internal static readonly string UpdateJobExpireAtStatement =
            GetSingleFieldUpdateSql(nameof(_Job), nameof(_Job.ExpireAt), nameof(_Job.Id));

        /// <summary>
        ///     HQL statement by which the FetchedAt property of a JobQueue entity will be updated
        /// </summary>
        internal static readonly string UpdateJobQueueFetchedAtStatement =
            GetSingleFieldUpdateSql(nameof(_JobQueue), nameof(_JobQueue.FetchedAt), nameof(_JobQueue.Id));


        /// <summary>
        ///     HQL statement by which to update an aggregated counter based upon its key
        /// </summary>
        internal static readonly string UpdateAggregateCounterStatement = string.Format(
            "update {0} s set s.{1}=s.{1} + :{4}, s.{3}= case when s.{3} >  :{6} then s.{3} else :{6} end where s.{2} = :{5}",
            nameof(_AggregatedCounter).WrapObjectName(), nameof(_AggregatedCounter.Value).WrapObjectName(),
            nameof(_AggregatedCounter.Key).WrapObjectName(),
            nameof(_AggregatedCounter.ExpireAt).WrapObjectName(), ValueParameterName, IdParameterName,
            ValueParameter2Name);


        /// <summary>
        ///     HQL statements by which to set ExpireAt property of Hash, Set or List entities
        /// </summary>
        internal static readonly Dictionary<Type, string> SetExpireAtByKeyStatementDictionary =
            new Dictionary<Type, string>
            {
                {typeof(_Set), GetSetExpireAtByKeyStatement<_Set>()},
                {typeof(_Hash), GetSetExpireAtByKeyStatement<_Hash>()},
                {typeof(_List), GetSetExpireAtByKeyStatement<_List>()}
            };

      

        internal static readonly string SetJobParameterStatement = string.Format(
            "update {0} set {1}=:value where {2}.{3}=:id and {4}=:name",
            nameof(_JobParameter), nameof(_JobParameter.Value), nameof(_JobParameter.Job), nameof(_Job.Id),
            nameof(_JobParameter.Name));

        internal static readonly string AnnounceServerStatement = string.Format(
            "update {0} set {1}=:data, {2}=:lastheartbeat where {3}=:id", nameof(_Server), nameof(_Server.Data),
            nameof(_Server.LastHeartbeat), nameof(_Server.Id));
#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif
        /// <summary>
        ///     Generate HQL to update a single property of an entity based on matched some column to a value.
        /// </summary>
        /// <param name="entityTypeName">The type name of the entity</param>
        /// <param name="updatedProperty">The property being updated</param>
        /// <param name="idProperty">The property to match against that uniquely identifies the entity</param>
        /// <returns></returns>
        private static string GetSingleFieldUpdateSql(string entityTypeName, string updatedProperty, string idProperty)
        {
            return string.Format("update {0} set {1}=:{3} where {2}=:{4}", entityTypeName.WrapObjectName(),
                updatedProperty.WrapObjectName(), idProperty.WrapObjectName(),
                ValueParameterName,
                IdParameterName);
        }
#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif

        /// <summary>
        ///     do an upsert into a table
        /// </summary>
        /// <typeparam name="T">The entity type to upsert</typeparam>
        /// <param name="session">a SessionWrapper instance to act upon</param>
        /// <param name="matchFunc">A function that returns a single instance of T</param>
        /// <param name="changeAction">A delegate that changes specified properties of instance of T </param>
        /// <param name="keysetAction">A delegate that sets the primary key properties of instance of T if we have to do an upsert</param>
        public static void UpsertEntity<T>(this StatelessSessionWrapper session, Expression<Func<T, bool>> matchFunc,
            Action<T> changeAction,
            Action<T> keysetAction) where T : new()
        {
            var entity = session.Query<T>().SingleOrDefault(matchFunc);
            if (entity == null)
            {
                entity = new T();
                keysetAction(entity);
                changeAction(entity);
                session.Insert(entity);
            }
            else
            {
                changeAction(entity);
                session.Update(entity);
            }


        }
 
 
#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif
        /// <summary>
        ///     delete entities that implement IInt32Id, by using the value stored in their Id property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session">Sessionwrapper instance to act upon</param>
        /// <param name="ids">Collection of ids to delete</param>
        /// <returns>the number of rows deleted</returns>
        public static long DeleteByInt32Id<T>(this StatelessSessionWrapper session, ICollection<int> ids) where T : IInt32Id
        {
            if (!ids.Any())
                return 0;

            var count = 0;
            for (var i = 0; i < ids.Count; i += DeleteBatchSize)
            {
                var batch = ids.Skip(i).Take(DeleteBatchSize).ToList();
                count += session.Query<T>().Where(j => batch.Contains(j.Id)).Delete();
            }

            return count;
        }

#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif
        /// <summary>
        ///     Generate a HQL statement that sets ExpireAt property of entity that implements IExpirableWithKey
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <returns>The HQL statement</returns>
        private static string GetSetExpireAtByKeyStatement<T>() where T : IExpirableWithKey
        {
            return string.Format("update {0} set {1}=:{2} where {3}=:{4}", typeof(T).Name.WrapObjectName(),
                nameof(IExpirable.ExpireAt).WrapObjectName(), ValueParameterName,
                nameof(IExpirableWithKey.Key).WrapObjectName(),
                IdParameterName);
        }
 
 
#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif
        public static T WrapForTransaction<T>(Func<T> safeFunc)
        {
            try
            {
                return safeFunc();
            }
            catch (AssertionFailure)
            {
                //do nothing
            }
            catch (TransactionException)
            {
                //do nothing
            }
            catch (GenericADOException)
            {
                //do nothing
            }

            return default;
        }

#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif


        public static T WrapForDeadlock<T>(CancellationToken cancellationToken, Func<T> safeAction,
            FluentNHibernateStorageOptions options)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return safeAction();
                }
                catch (Exception ex)
                {
                    if (ex.Message.IndexOf("deadlock", StringComparison.InvariantCultureIgnoreCase) < 0)
                        throw;

                    cancellationToken.Wait(options.DeadlockRetryInterval);
                }
            }
        }
#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif

        public static void WrapForDeadlock(CancellationToken cancellationToken, Action safeAction,
            FluentNHibernateStorageOptions options)
        {
            WrapForDeadlock(cancellationToken, () =>
            {
                safeAction();
                return true;
            }, options);
        }

#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif
        public static void WrapForTransaction(Action safeAction)
        {
            WrapForTransaction(() =>
            {
                safeAction();
                return true;
            });
        }
    }
}