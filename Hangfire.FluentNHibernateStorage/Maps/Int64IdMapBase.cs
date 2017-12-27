using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public abstract class Int64IdMapBase<T> : ClassMap<T> where T : IInt64Id
    {
        protected Int64IdMapBase()
        {
            LazyLoad();
            Id(i => i.Id).Column(Constants.ColumnNames.Id.WrapObjectName()).GeneratedBy.Identity();
        }
    }
}