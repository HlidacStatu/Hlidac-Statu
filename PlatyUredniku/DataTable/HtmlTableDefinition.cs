using System;

namespace PlatyUredniku.DataTable;

public class HtmlTableDefinition
{
    public class ColumnAttribute : Attribute
    {
        public ColumnType Type { get; }
        public string Name { get; }

        public ColumnAttribute(ColumnType type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public enum ColumnType
    {
        Text,
        Number,
        Price,
        Hidden
    }
}