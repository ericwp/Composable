﻿using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore.Aggregates;
using Composable.System.Threading;
using Composable.System.Transactions;
using Composable.SystemExtensions.Threading;

namespace Composable.Messaging.Buses
{
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    partial class ServiceBusSession : IServiceBusSession, ILocalServiceBusSession
    {
        readonly IInterprocessTransport _transport;
        readonly CommandScheduler _commandScheduler;
        readonly IMessageHandlerRegistry _handlerRegistry;
        readonly ISingleContextUseGuard _contextGuard;

        public ServiceBusSession(IInterprocessTransport transport, CommandScheduler commandScheduler, IMessageHandlerRegistry handlerRegistry)
        {
            _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard());
            _transport = transport;
            _commandScheduler = commandScheduler;
            _handlerRegistry = handlerRegistry;
        }

        async Task<TResult> IRemoteServiceBusSession.GetRemoteAsync<TResult>(MessagingApi.IQuery<TResult> query)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSend(query);
            return query is ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : await _transport.DispatchAsync(query).NoMarshalling();
        }

        TResult IRemoteServiceBusSession.GetRemote<TResult>(MessagingApi.IQuery<TResult> query) => ((IRemoteServiceBusSession)this).GetRemoteAsync(query).ResultUnwrappingException();

        void IEventstoreEventPublisher.Publish(MessagingApi.Remote.ExactlyOnce.IEvent @event) => TransactionScopeCe.Execute(() =>
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSend(@event);
            _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
            _transport.DispatchIfTransactionCommits(@event);
        });

        void IRemoteServiceBusSession.PostRemote(MessagingApi.Remote.ExactlyOnce.ICommand command) => TransactionScopeCe.Execute(() =>
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSend(command);
            CommandValidator.AssertCommandIsValid(command);
            _transport.DispatchIfTransactionCommits(command);
        });

        void IRemoteServiceBusSession.SchedulePostRemote(DateTime sendAt, MessagingApi.Remote.ExactlyOnce.ICommand command) => TransactionScopeCe.Execute(() =>
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _commandScheduler.Schedule(sendAt, command);
        });

        Task<TResult> IRemoteServiceBusSession.PostRemoteAsync<TResult>(MessagingApi.Remote.ExactlyOnce.ICommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return _transport.DispatchIfTransactionCommitsAsync(command);
        });

        TResult IRemoteServiceBusSession.PostRemote<TResult>(MessagingApi.Remote.ExactlyOnce.ICommand<TResult> command) => ((IRemoteServiceBusSession)this).PostRemoteAsync(command).ResultUnwrappingException();

        TResult ILocalServiceBusSession.PostLocal<TResult>(MessagingApi.Remote.ExactlyOnce.ICommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return _handlerRegistry.GetCommandHandler(command).Invoke(command);
        });

        void ILocalServiceBusSession.PostLocal(MessagingApi.Remote.ExactlyOnce.ICommand command) => TransactionScopeCe.Execute(() =>
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _handlerRegistry.GetCommandHandler(command).Invoke(command);
        });

        TResult ILocalServiceBusSession.GetLocal<TResult>(MessagingApi.IQuery<TResult> query)
        {
            MessageInspector.AssertValidToSend(query);
            _contextGuard.AssertNoContextChangeOccurred(this);
            return query is ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : _handlerRegistry.GetQueryHandler(query).Invoke(query);
        }
    }
}
