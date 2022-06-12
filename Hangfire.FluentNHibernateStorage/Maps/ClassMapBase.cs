using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal abstract class ClassMapBase<T> : ClassMap<T>
    {
        protected ClassMapBase()
        {
            Table(Tablename.WrapObjectName());
        }

        public abstract string Tablename { get; }
    }
}