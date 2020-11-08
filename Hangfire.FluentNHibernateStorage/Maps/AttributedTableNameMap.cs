using FluentNHibernate.Mapping;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public abstract class AttributedTableNameMap<T> : ClassMap<T>
    {
        private string _tableName = null;

        protected  AttributedTableNameMap()
        {
            Table(GetTableName().WrapObjectName());
        }
        public string GetTableName()
        {
            if (_tableName == null)
            {
                _tableName = string.Concat(FluentNHibernateStorageOptions.DefaultTablePrefix,
                    TableNameHelper.GetTableName(typeof(T)));
            }
            return _tableName;
        }
        
    }
}