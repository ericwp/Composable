﻿using System;
using System.Collections.Generic;
using Composable.Messaging.Events;

namespace Composable.Messaging.Buses
{
    ///<summary>Dispatches messages within a process.</summary>
    public interface IInProcessServiceBus
    {
        void Publish(IEvent anEvent);
        TResult Get<TResult>(IQuery<TResult> query) where TResult : IQueryResult;
        void Send(ICommand message);
        bool Handles(object aMessage);
    }

    ///<summary>Dispatches messages between processes.</summary>
    public interface IInterProcessServiceBus
    {
        void SendAtTime(DateTime sendAt, ICommand message);
    }

    public interface IServiceBus : IInProcessServiceBus, IInterProcessServiceBus {}

    public interface IMessageSpy
    {
        IEnumerable<IMessage> DispatchedMessages { get; }
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(ICommand message);

        Func<IQuery<TResult>, TResult> GetQueryHandler<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        IEventDispatcher<IEvent> CreateEventDispatcher();

        bool Handles(object aMessage);
    }

    public interface IMessageHandlerRegistrar
    {
        // ReSharper disable UnusedMethodReturnValue.Global
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
        IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult> where TResult : IQueryResult;
        // ReSharper restore UnusedMethodReturnValue.Global
    }
}
