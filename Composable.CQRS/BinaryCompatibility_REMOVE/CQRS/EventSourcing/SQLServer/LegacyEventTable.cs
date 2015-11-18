﻿using System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    internal static class LegacyEventTable
    {
        public static string Name { get; } = "Events";

        internal static class Columns
        {
            public const string AggregateId = nameof(AggregateId);
            public const string AggregateVersion = nameof(AggregateVersion);
            public const string TimeStamp = nameof(TimeStamp);
            public const string InsertionOrder = nameof(InsertionOrder);
            public const string EventType = nameof(EventType);
            public const string EventTypeId = nameof(EventTypeId);
            public const string EventId = nameof(EventId);
            public const string Event = nameof(Event);
            public static string SqlTimeStamp = nameof(SqlTimeStamp);
        }        
    }
}