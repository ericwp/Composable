﻿using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.When_renaming_events.With_renaming_attribute
{
    [TestFixture]
    public class By_path_and_name
    {
        RenamingEventNameMapper _nameMapper;

        [SetUp]
        public void SetupMappingsForEventsWithNoRenamingAttribute()
        {
            _nameMapper = new RenamingEventNameMapper(
                eventTypes: Seq.OfTypes<Event1, Event2>(),
                renamers: new RenameEventsBasedOnEventRenamedAttributes());
        }

        [Test]
        public void Event_type_maps_to_event_name_with_name_and_path_replaced()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(Event1.OldFullName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(Event2.OldFullName);
        }

        [Test]
        public void Event_name_with_name_and_path_replaced_maps_to_event_type()
        {
            _nameMapper.GetType(Event1.OldFullName).Should().Be(typeof(Event1));
            _nameMapper.GetType(Event2.OldFullName).Should().Be(typeof(Event2));
        }

        [EventRenamedFrom(Name = OldName, Path = OldPath)] [TypeId("E81DB059-B1E5-4162-93CB-6F50ED84D37B")]class Event1 : AggregateRootEvent
        {
            const string OldName = "Event1Old";
            const string OldPath = "OldPath.";
            public static readonly string OldFullName = OldPath + OldName;
        }

        [EventRenamedFrom(Name = OldName, Path = OldPath)] [TypeId("E6F8F3A2-E234-4490-A87B-CF02A52BD8FF")]class Event2 : AggregateRootEvent
        {
            const string OldName = "Event2Old";
            const string OldPath = "OldPath.";
            public static readonly string OldFullName = OldPath + OldName;
        }
    }
}
