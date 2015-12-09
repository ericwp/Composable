﻿using System;
using System.IO;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.System;
using Composable.System.IO;
using JetBrains.Annotations;

namespace Composable.KeyValueStorage.Population
{
    [UsedImplicitly]
    public class AggregateIdFetcher
    {
        private readonly IEventStore _events;

        public AggregateIdFetcher(IEventStore events)
        {
            _events = events;
        }

        public Guid[] GetAll()
        {
            return _events.StreamAggregateIdsInCreationOrder().ToArray();
        }


        public Guid[] GetEntitiesCreatedAfter()
        {
            return _events.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().Select(e => e.AggregateRootId).Distinct().ToArray();
        }

        public Guid[] GetEntitiesFromFile(FileStream file)
        {
            return file.Lines()
                       .Select(row => row.Trim())
                       .Where(row => !row.IsNullOrWhiteSpace())
                       .Select(Guid.Parse)
                       .ToArray();
        }
    }
}
