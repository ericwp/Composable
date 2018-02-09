﻿namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class EventTypeTableSchemaManager : TableSchemaManager
    {
        internal override string Name { get; } = EventTypeTable.Name;

        internal override string CreateTableSql => $@"
CREATE TABLE [dbo].[{EventTypeTable.Name}](
	[{EventTypeTable.Columns.Id}] [int] IDENTITY(1,1) NOT NULL,
	[{EventTypeTable.Columns.EventType}] [UNIQUEIDENTIFIER] NOT NULL,
    CONSTRAINT [PK_{EventTypeTable.Columns.EventType}] PRIMARY KEY CLUSTERED 
    (
    	[Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [IX_Uniq_{EventTypeTable.Columns.EventType}] UNIQUE
    (
	    {EventTypeTable.Columns.EventType}
    )
)";
    }
}