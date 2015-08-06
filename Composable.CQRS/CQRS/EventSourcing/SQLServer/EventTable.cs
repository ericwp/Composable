﻿namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal static class EventTable
    {
        public static string Name { get; } = "Events";

        internal static class Columns
        {
            public const string Id = nameof(Id);
            public const string AggregateId = nameof(AggregateId);
            public const string AggregateVersion = nameof(AggregateVersion);
            public const string TimeStamp = nameof(TimeStamp);
            public const string SqlTimeStamp = nameof(SqlTimeStamp);
            public const string EventType = nameof(EventType);
            public const string EventTypeId = nameof(EventTypeId);
            public const string EventId = nameof(EventId);
            public const string Event = nameof(Event);
        }        
    }
}