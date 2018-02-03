﻿using System;
using System.Collections.Generic;
using Composable.Serialization;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable MemberCanBePrivate.Local we want the inspection of the objects to include all properties...
// ReSharper disable MemberCanBePrivate.Global

namespace Composable.Tests.Serialization.BinarySerializeds
{
    [TestFixture, NCrunch.Framework.Serial] public class Performance_tests
    {
        HasAllPropertyTypes _instance;
        byte[] _serialized;
        [SetUp] public void SetupTask()
        {
            _instance = HasAllPropertyTypes.CreateInstance();

            _instance.RecursiveArrayProperty = new[]
                                              {
                                                  HasAllPropertyTypes.CreateInstance(),
                                                  null,
                                                  HasAllPropertyTypes.CreateInstance()
                                              };

            _instance.RecursiveListProperty = new List<HasAllPropertyTypes>()
                                             {
                                                 HasAllPropertyTypes.CreateInstance(),
                                                 null,
                                                 HasAllPropertyTypes.CreateInstance()
                                             };

            _serialized = _instance.Serialize();

            //Warmup
            RunScenario(() => DefaultConstructor(1), 1000);
            RunScenario(() => BinaryCreateInstance(1), 1000);
            RunScenario(() => JsonRoundTrip(_instance, 1), 1000);
            RunScenario(() => BinaryRoundTrip(_instance, 1), iterations: 1000);

        }

        [Test] public void Instance_with_recursive_list_and_array_property_with_one_null_value_roundtrip_5_times_faster_than_NewtonSoft()
        {
            const int iterations = 1_000;


            var jsonSerializationTime = RunScenario(() => JsonRoundTrip(_instance, 1), iterations);

            var maxTotal = TimeSpan.FromMilliseconds(jsonSerializationTime.TotalMilliseconds / 5);

            var binarySerializationTime = RunScenario(() => BinaryRoundTrip(_instance, 1), iterations.InstrumentationSlowdown(5), maxTotal:maxTotal);

            Console.WriteLine($"Binary: {binarySerializationTime.TotalMilliseconds}, JSon: {jsonSerializationTime.TotalMilliseconds}");
        }

        [Test] public void _005_Constructs_1_00_000_instances_within_40_percent_of_default_constructor_time()
        {
            var constructions = 1_00_000;
            var defaultConstructor = RunScenario(() => DefaultConstructor(1), constructions.InstrumentationSlowdown(4.7));
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * 1.4);
            RunScenario(() => BinaryCreateInstance(1), constructions.InstrumentationSlowdown(4.7), maxTotal: maxTime );
        }

        [Test] public void _010_Serializes_10_000_times_in_100_milliseconds() =>
            RunScenario(() => BinarySerialize(_instance, 1), 10_000.InstrumentationSlowdown(6.5), maxTotal:100.Milliseconds());

        [Test] public void _020_DeSerializes_10_000_times_in_130_milliseconds() =>
                RunScenario(() => BinaryDeSerialize(1), iterations: 10_000.InstrumentationSlowdown(5.5), maxTotal:130.Milliseconds());

        [Test] public void _030_Roundtrips_10_000_times_in_220_milliseconds() =>
            RunScenario(() => BinaryRoundTrip(_instance, 1), iterations: 10_000.InstrumentationSlowdown(6), maxTotal:220.Milliseconds());

        //ncrunch: no coverage start

        static TimeSpan RunScenario(Action action, int iterations, TimeSpan? maxTotal = null)
        {
            if(maxTotal != null)
            {
                return TimeAsserter.Execute(action, iterations: iterations, maxTotal: maxTotal).Total;
            } else
            {
                return StopwatchExtensions.TimeExecution(action, iterations: iterations).Total;
            }
        }

        static void JsonRoundTrip(HasAllPropertyTypes instance, int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                var data = JsonConvert.SerializeObject(instance);
                instance = JsonConvert.DeserializeObject<HasAllPropertyTypes>(data);
            }
        }

        static void BinaryRoundTrip(HasAllPropertyTypes instance, int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                var data = instance.Serialize();
                instance = BinarySerialized<HasAllPropertyTypes>.Deserialize(data);
            }
        }

        static void BinarySerialize(HasAllPropertyTypes instance, int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                instance.Serialize();
            }
        }

        void BinaryDeSerialize(int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                BinarySerialized<HasAllPropertyTypes>.Deserialize(_serialized);
            }
        }

        static void BinaryCreateInstance(int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                BinarySerialized<HasAllPropertyTypes>.Construct();
            }
        }

        static void DefaultConstructor(int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                // ReSharper disable once ObjectCreationAsStatement
                new HasAllPropertyTypes();
            }
        }

        //ncrunch: no coverage end

    }
}
