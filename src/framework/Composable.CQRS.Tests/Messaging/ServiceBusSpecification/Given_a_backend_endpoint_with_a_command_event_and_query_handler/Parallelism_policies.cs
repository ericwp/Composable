﻿using System.Linq;
using System.Threading;
using Composable.Messaging.Buses;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Parallelism_policies : Fixture
    {
        [Fact] public void Command_handler_executes_on_different_thread_from_client_sending_command()
        {
            ClientEndpoint.ExecuteRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

            CommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
            CommandHandlerThreadGate.PassedThrough.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Event_handler_executes_on_different_thread_from_client_publishing_event()
        {
            ClientEndpoint.ExecuteRequestInTransaction(session => session.Publish(new MyExactlyOnceEvent()));

            EventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
            EventHandlerThreadGate.PassedThrough.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Query_handler_executes_on_different_thread_from_client_sending_query()
        {
            ClientEndpoint.ExecuteRequest(session => session.Get(new MyQuery()));

            QueryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                  .PassedThrough.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Five_query_handlers_can_execute_in_parallel_when_using_QueryAsync()
        {
            CloseGates();
            TaskRunner.StartTimes(5, () => ClientEndpoint.ExecuteRequestAsync(session => session.GetAsync(new MyQuery())));

            QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
        }

        [Fact] public void Five_query_handlers_can_execute_in_parallel_when_using_Query()
        {
            CloseGates();
            TaskRunner.StartTimes(5, () => ClientEndpoint.ExecuteRequest(session => session.Get(new MyQuery())));

            QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
        }

        [Fact] public void Two_event_handlers_cannot_execute_in_parallel()
        {
            CloseGates();
            ClientEndpoint.ExecuteRequestInTransaction(session => session.Publish(new MyExactlyOnceEvent()));
            ClientEndpoint.ExecuteRequestInTransaction(session => session.Publish(new MyExactlyOnceEvent()));

            EventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                  .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public void Two_command_handlers_cannot_execute_in_parallel()
        {
            CloseGates();

            ClientEndpoint.ExecuteRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));
            ClientEndpoint.ExecuteRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

            CommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                    .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public void Command_handler_cannot_execute_if_event_handler_is_executing()
        {
            CloseGates();

            ClientEndpoint.ExecuteRequestInTransaction(session => session.Publish(new MyExactlyOnceEvent()));
            EventHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            ClientEndpoint.ExecuteRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

            CommandHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public void Command_handler_with_result_cannot_execute_if_event_handler_is_executing()
        {
            CloseGates();
            ClientEndpoint.ExecuteRequestInTransaction(session => session.Publish(new MyExactlyOnceEvent()));
            EventHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            var resultTask = ClientEndpoint.ExecuteRequestAsync(session => session.PostAsync(new MyAtMostOnceCommandWithResult())); //awaiting later
            CommandHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);

            OpenGates();
            resultTask.Result.Should().NotBe(null);
        }

        [Fact] public void Event_handler_cannot_execute_if_command_handler_is_executing()
        {
            CloseGates();
            ClientEndpoint.ExecuteRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));
            CommandHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            ClientEndpoint.ExecuteRequestInTransaction(session => session.Publish(new MyExactlyOnceEvent()));
            EventHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public void Event_handler_cannot_execute_if_command_handler_with_result_is_executing()
        {
            CloseGates();

            var resultTask = ClientEndpoint.ExecuteRequestAsync(session => session.PostAsync(new MyAtMostOnceCommandWithResult())); //awaiting later
            CommandHandlerWithResultThreadGate.AwaitQueueLengthEqualTo(1);

            ClientEndpoint.ExecuteRequestInTransaction(session => session.Publish(new MyExactlyOnceEvent()));
            EventHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);

            OpenGates();
            resultTask.Result.Should().NotBe(null);
        }
    }
}
