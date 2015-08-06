﻿namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class EventTypeTableSchemaManager : TableSchemaManager
    {
        private EventTypeTable EventTypeTable { get; } = new EventTypeTable();

        override public string Name { get; } = "EventType";
  

        override public string CreateTableSql => $@"
    CREATE TABLE [dbo].[{EventTypeTable.Name}](
	[{EventTypeTable.Columns.Id}] [int] IDENTITY(1,1) NOT NULL,
	[{EventTypeTable.Columns.EventType}] [varchar](300) NOT NULL,
    CONSTRAINT [PK_{EventTypeTable.Columns.EventType}] PRIMARY KEY CLUSTERED 
    (
    	[Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
";
    }
}