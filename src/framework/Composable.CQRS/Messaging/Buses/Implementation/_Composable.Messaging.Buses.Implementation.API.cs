﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    interface IOutbox
    {
        void SendAtTime(DateTime sendAt, IDomainCommand command);
        void Publish(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query);
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);

        void Send(IDomainCommand command);
        Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command) where TResult : IMessage;
        void Start();
        void Stop();
    }


    interface IInterprocessTransport
    {
        void Stop();
        void Start();
        void Dispatch(IEvent message);
        void Dispatch(IDomainCommand command);
        Task<TCommandResult> Dispatch<TCommandResult>(IDomainCommand<TCommandResult> command) where TCommandResult : IMessage;
        Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> command);
    }

    interface IInbox
    {
    }
}