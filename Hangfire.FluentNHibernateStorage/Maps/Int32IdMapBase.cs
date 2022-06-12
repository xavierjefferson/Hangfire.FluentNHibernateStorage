using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal abstract class Int32IdMapBase<T> : ClassMapBase<T> where T : IInt32Id
    {
        protected Int32IdMapBase()
        {
            LazyLoad();
            this.MapIntIdColumn();
        }
    }
}