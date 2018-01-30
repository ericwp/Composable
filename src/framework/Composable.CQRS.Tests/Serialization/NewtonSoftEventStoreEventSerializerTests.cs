﻿using System;
using System.Linq;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Composable.Tests.Serialization
{
    [TestFixture, Performance]
    public class NewtonSoftEventStoreEventSerializerTests
    {
        readonly IEventStoreSerializer _eventSerializer = new NewtonSoftEventStoreSerializer(new TypeMapper());

        class TestEvent : AggregateEvent
        {
            [UsedImplicitly]
            public TestEvent() { }

            public TestEvent(string test1, string test2)
            {
                Test1 = test1;
                Test2 = test2;
            }

            public TestEvent(
                string test1,
                string test2,
                Guid eventId,
                int aggregateVersion,
                Guid aggregateId,
                long insertionOrder,
                long? replaces,
                long? insertBefore,
                long? insertAfter,
                DateTime utcTimeStamp)
#pragma warning disable CS0618 // Type or member is obsolete
                : base(
                       aggregateId: aggregateId,
                       aggregateVersion: aggregateVersion,
                       eventId: eventId,
                       insertAfter: insertAfter,
                       insertBefore: insertBefore,
                       replaces: replaces,
                       insertionOrder: insertionOrder,
                       utcTimeStamp: utcTimeStamp
                      )
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Test1 = test1;
                Test2 = test2;
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public string Test1 { [UsedImplicitly] get; private set; }
            // ReSharper disable once MemberCanBePrivate.Local
            public string Test2 { [UsedImplicitly] get; private set; }
        }


        [Test]
        public void IgnoresAllIAggregateEventProperties()
        {
            var eventWithAllValuesSet = new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateId:  Guid.NewGuid(),
                                            aggregateVersion:  2,
                                            eventId: Guid.NewGuid(),
                                            insertAfter:  10,
                                            insertBefore:  20,
                                            replaces: 30,
                                            insertionOrder: 40,
                                            utcTimeStamp: DateTime.Now + 1.Minutes());

            var eventWithOnlySubclassValues = new TestEvent("Test1", "Test2")
                                              {
                                                  EventId = Guid.Empty,
                                                  UtcTimeStamp = DateTime.MinValue
                                              };

            var eventWithAllValuesJson = _eventSerializer.Serialize(eventWithAllValuesSet);
            var eventWithOnlySubclassValuesJson = _eventSerializer.Serialize(eventWithOnlySubclassValues);
            var roundTripped = _eventSerializer.Deserialize(typeof(TestEvent), eventWithAllValuesJson);

            SafeConsole.WriteLine(eventWithAllValuesJson);

            eventWithAllValuesJson.Should().Be(@"{
  ""Test1"": ""Test1"",
  ""Test2"": ""Test2""
}");
            eventWithAllValuesJson.Should().Be(eventWithOnlySubclassValuesJson);

            roundTripped.ShouldBeEquivalentTo(eventWithOnlySubclassValues,
                config => config.Excluding(@event => @event.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                        .Excluding(@event => @event.EventId)
                        .Excluding(@event => @event.MessageId)
                );
        }

        [Test] public void Should_roundtrip_simple_event_1000_times_in_15_milliseconds_with_new_instance_for_each_serialization()
        {
            var @event = new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateId: Guid.NewGuid(),
                                            aggregateVersion: 2,
                                            eventId: Guid.NewGuid(),
                                            insertAfter: 10,
                                            insertBefore: 20,
                                            replaces: 30,
                                            insertionOrder: 40,
                                            utcTimeStamp: DateTime.Now + 1.Minutes());

            //Warmup
            _eventSerializer.Deserialize(typeof(TestEvent), _eventSerializer.Serialize(@event));

            TimeAsserter.Execute(
                                 () =>
                                 {
                                     var eventJson = new NewtonSoftEventStoreSerializer(new TypeMapper()).Serialize(@event);
                                     new NewtonSoftEventStoreSerializer(new TypeMapper()).Deserialize(typeof(TestEvent), eventJson);
                                 },
                                 iterations:1000,
                                 maxTotal: 15.Milliseconds()
                                );
        }

        [Test] public void Should_roundtrip_simple_event_within_70_percent_of_default_serializer_performance_given_all_new_serializer_instances()
        {
            const int iterations = 10000;
            const double allowedSlowdown = 1.7;

            var events = 1.Through(iterations).Select( index =>  new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateId: Guid.NewGuid(),
                                            aggregateVersion: 2,
                                            eventId: Guid.NewGuid(),
                                            insertAfter: 10,
                                            insertBefore: 20,
                                            replaces: 30,
                                            insertionOrder: 40,
                                            utcTimeStamp: DateTime.Now + 1.Minutes())).ToList();

            var settings = NewtonSoftEventStoreSerializer.JsonSettings;

            //Warmup
            _eventSerializer.Deserialize(typeof(TestEvent), _eventSerializer.Serialize(events.First()));
            JsonConvert.DeserializeObject<TestEvent>(JsonConvert.SerializeObject(events.First(), settings), settings);

            var defaultSerializerPerformanceNumbers = StopwatchExtensions.TimeExecution(() =>
                                                                                        {
                                                                                            var eventJson = events.Select(@this => JsonConvert.SerializeObject(@this, settings))
                                                                                                                  .ToList();
                                                                                            eventJson.ForEach(@this => JsonConvert.DeserializeObject<TestEvent>(@this, settings));
                                                                                        });

            var allowedTime = TimeSpan.FromMilliseconds(defaultSerializerPerformanceNumbers.TotalMilliseconds * allowedSlowdown);

            TimeAsserter.Execute(() =>
                                 {
                                     var eventJson = events.Select(new NewtonSoftEventStoreSerializer(new TypeMapper()).Serialize)
                                                            .ToList();
                                     eventJson.ForEach(@this => new NewtonSoftEventStoreSerializer(new TypeMapper()).Deserialize(typeof(TestEvent), @this));
                                 },
                                 maxTotal: allowedTime);
        }
    }
}