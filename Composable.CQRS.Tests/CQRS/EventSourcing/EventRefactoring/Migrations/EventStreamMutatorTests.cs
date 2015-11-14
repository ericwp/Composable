﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Windsor;
using Composable.CQRS.Windsor.Testing;
using Composable.ServiceBus;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using TestAggregates;
using TestAggregates.Events;


namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class InMemoryEventStoreEventStreamMutatorTests : EventStreamMutatorTests
    {
        public InMemoryEventStoreEventStreamMutatorTests() : base(typeof(InMemoryEventStore)){}
    }

    public class SqlServerEventStoreEventStreamMutatorTests : EventStreamMutatorTests
    {
        public SqlServerEventStoreEventStreamMutatorTests() : base(typeof(SqlServerEventStore)) { }
    }

    [TestFixture, ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public abstract class EventStreamMutatorTests : EventStreamMutatorTestsBase
    {
        protected EventStreamMutatorTests(Type eventStoreType) : base(eventStoreType) {}


        [Test]
        public void Replacing_E1_with_E2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                Seq.OfTypes<Ec1, E2, Ef, Ef>(),
                Replace<E1>.With<E2>());
        }

        [Test]
        public void Replacing_E1_with_E2_at_end_of_stream()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2>(),
                Replace<E1>.With<E2>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, E3, Ef>(),
                Replace<E1>.With<E2, E3>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef, Ef, Ef>(),
                Seq.OfTypes<Ec1, E2, E3, Ef, Ef, Ef, Ef>(),
                Replace<E1>.With<E2, E3>());
        }

        [Test]
        public void Replacing_E1_with_E2_then_irrelevant_migration()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, Ef>(),
                Replace<E1>.With<E2>(),
                Replace<E1>.With<E5>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, E3, Ef>(),
                Replace<E1>.With<E2, E3>(),
                Replace<E1>.With<E5>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_E2_with_E4()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E4, E3, Ef>(),
                Replace<E1>.With<E2,E3>(),
                Replace<E2>.With<E4>());
        }


        [Test]
        public void Inserting_E3_before_E1()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E1, Ef>(),
                Before<E1>.Insert<E3>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E4, E1, Ef>(),
                Before<E1>.Insert<E3, E4>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E3()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E5, E3, E4, E1>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E3>.Insert<E5>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E5, E4, E1, Ef>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E4>.Insert<E5>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E5, E6, E1, Ef>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E4>.Insert<E5>(),
                Replace<E3>.With<E6>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E3_2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                Seq.OfTypes<Ec1, E5, E3, E4, E1, Ef, Ef>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E3>.Insert<E5>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4_2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                Seq.OfTypes<Ec1, E3, E5, E4, E1, Ef, Ef>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E4>.Insert<E5>());
        }

        [Test]
        public void Inserting_E2_after_E1()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                Seq.OfTypes<Ec1, E1, E2, Ef, Ef>(),
                After<E1>.Insert<E2>());
        }

        [Test]
        public void Inserting_E2_after_E1_at_end_of_stream()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E1, E2>(),
                After<E1>.Insert<E2>());
        }
    }
}