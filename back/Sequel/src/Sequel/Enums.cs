﻿using System.Text.Json.Serialization;

namespace Sequel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DBMS
    {
        MySQL,
        MariaDB,
        Oracle,
        PostgreSQL,
        SQLite,
        SQLServer,
        Cassandra,
        CockroachDB
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Env
    {
        Development,
        Testing,
        Staging,
        UAT,
        Demo,
        Production
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TreeViewNodeType
    {
        Database,
        Schema,
        Table,
        Function,
        Column,

        // Group label
        Schemas,
        Tables,
        Functions,
        Columns,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum QueryResponseStatus
    {
        Succeeded,
        Canceled,
        Failed,
    }

    public enum CompletionItemKind
    {
        Method = 0,
        Function = 1,
        Constructor = 2,
        Field = 3,
        Variable = 4,
        Class = 5,
        Struct = 6,
        Interface = 7,
        Module = 8,
        Property = 9,
        Event = 10,
        Operator = 11,
        Unit = 12,
        Value = 13,
        Constant = 14,
        Enum = 15,
        EnumMember = 16,
        Keyword = 17,
        Text = 18,
        Color = 19,
        File = 20,
        Reference = 21,
        Customcolor = 22,
        Folder = 23,
        TypeParameter = 24,
        Snippet = 25
    }
}
