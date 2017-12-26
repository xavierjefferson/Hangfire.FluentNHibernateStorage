using System;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class TableNameRootAttribute : Attribute
    {
        public TableNameRootAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}