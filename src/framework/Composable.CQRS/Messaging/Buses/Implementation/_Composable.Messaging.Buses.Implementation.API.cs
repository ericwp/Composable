﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    interface IInterprocessTransport
    {
        void SendAtTime(DateTime sendAt, ICommand command);
        void Publish(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult;
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        void Send(ICommand command);
        Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage;
        void Start();
        void Stop();
    }


    interface IRouter
    {
        IInbox RouteFor(ICommand command);
        IEnumerable<IInbox> RouteFor(IEvent @event);
        IInbox RouteFor(IQuery query);
    }

    interface IInbox
    {
        Task<object> Dispatch(IMessage message);
    }
}