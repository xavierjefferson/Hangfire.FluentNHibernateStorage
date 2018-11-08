using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public abstract class Int32IdMapBase<T> : ClassMap<T> where T : IInt32Id
    {
        protected Int32IdMapBase()
        {
            LazyLoad();
            Id(i => i.Id).Column(Constants.ColumnNames.Id.WrapObjectName()).GeneratedBy.Identity();
        }
    }
}