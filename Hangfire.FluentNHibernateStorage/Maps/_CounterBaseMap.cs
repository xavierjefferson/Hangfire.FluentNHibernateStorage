using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal abstract class _CounterBaseMap<T> : Int32IdMapBase<T> where T : CounterBase
    {
        protected _CounterBaseMap()
        {
            Table(Tablename.WrapObjectName());
            //id is mapped in base class
            var tmp = this.MapStringKeyColumn().Length(100);
            if (KeyIsUnique)
                tmp.UniqueKey($"UX_Hangfire_{Tablename}_Key");
            else
                tmp.Index($"IX_Hangfire_{Tablename}_Key");
            this.MapIntValueColumn();
            this.MapExpireAt();
        }


        public abstract bool KeyIsUnique { get; }
    }
}