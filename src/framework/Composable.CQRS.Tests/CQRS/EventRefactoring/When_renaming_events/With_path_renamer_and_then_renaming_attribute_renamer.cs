﻿using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.When_renaming_events
{
    public class With_path_renamer_and_then_renaming_attribute_renamer
    {
        const string OldPath = "Some.Old.Path.";

        RenamingEventNameMapper _nameMapper;

        [SetUp]
        public void SetupMappingsForEventsWithNoRenamingAttribute()
        {
            _nameMapper = new RenamingEventNameMapper(
                Seq.OfTypes<Event1, Event2>(),
                new EventPathRenamer(
                    oldPath: OldPath,
                    eventAtNewPath: typeof(Event1)
                    ),
                new RenameEventsBasedOnEventRenamedAttributes());
        }

        [Test]
        public void Event_type_maps_to_event_name_with_path_replaced_by_path_replacer_and_name_replaced_by_attribute_name()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(Event1.OldName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(Event2.OldName);
        }

        [Test]
        public void Event_name_with_path_replaced_by_path_replacer_and_name_replaced_by_attribute_name_maps_to_event_type()
        {
            _nameMapper.GetType(Event1.OldName).Should().Be(typeof(Event1));
            _nameMapper.GetType(Event2.OldName).Should().Be(typeof(Event2));
        }

        [TypeId("A6ABDC1B-5D5C-4A0B-8816-2ED38F8AC56D")][EventRenamedFrom(Name = "OldEvent1")] class Event1 : DomainEvent
        {
            public const string OldName = OldPath + "OldEvent1";
        }

        [TypeId("A77340CA-69C7-48CE-AC2A-34F18B580CCD")][EventRenamedFrom(Name = "OldEvent2")] class Event2 : DomainEvent
        {
            public const string OldName = OldPath + "OldEvent2";
        }
    }
}