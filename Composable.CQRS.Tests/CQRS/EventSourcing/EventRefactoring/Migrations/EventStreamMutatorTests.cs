﻿using Composable.System.Linq;
using NUnit.Framework;
using TestAggregates.Events;


namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [TestFixture]
    public class EventStreamMutatorTests : EventStreamMutatorTestsBase
    {
        [Test]
        public void Replacing_one_event_with_one_event()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, Ef>(),
                ReplaceEventType<E1>.With<E2>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, E3, Ef>(),
                ReplaceEventType<E1>.With<E2, E3>());
        }

        [Test]
        public void Replacing_E1_with_E2_then_irrelevant_migration()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, Ef>(),
                ReplaceEventType<E1>.With<E2>(),
                ReplaceEventType<E1>.With<E5>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, E3, Ef>(),
                ReplaceEventType<E1>.With<E2, E3>(),
                ReplaceEventType<E1>.With<E5>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_E2_with_E4()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E4, E3, Ef>(),
                ReplaceEventType<E1>.With<E2,E3>(),
                ReplaceEventType<E2>.With<E4>());
        }


        [Test]
        public void Inserting_E3_before_E1()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E1, Ef>(),
                BeforeEventType<E1>.Insert<E3>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E4, E1, Ef>(),
                BeforeEventType<E1>.Insert<E3, E4>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E3()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E5, E3, E4, E1>(),
                BeforeEventType<E1>.Insert<E3, E4>(),
                BeforeEventType<E3>.Insert<E5>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E3, E5, E4, E1>(),
                BeforeEventType<E1>.Insert<E3, E4>(),
                BeforeEventType<E4>.Insert<E5>());
        }
    }
}