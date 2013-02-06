﻿using System;
using System.Transactions;
using Composable.ServiceBus;
using NServiceBus.Saga;

namespace Composable.CQRS.Command
{
    /// <summary>
    /// Base class for implementing nServiceBus Saga, inherit this and then add IHandleMessages for your saga steps
    /// </summary>
    /// <typeparam name="TSagaData">Saga data storage class</typeparam>
    /// <typeparam name="TCommand">The command that should start the saga</typeparam>
    public abstract class SagaBase<TSagaData, TCommand> : Saga<TSagaData>, IAmStartedByMessages<TCommand>
        where TSagaData : ISagaEntity
        where TCommand : Command
    {
        private readonly IServiceBus _bus;

        protected SagaBase()
        { }

        protected SagaBase(IServiceBus bus)
        {
            _bus = bus;
        }

        public virtual void Handle(TCommand message)
        {
            try
            {
                HandleBaseStartCommand(message);
            }
            catch (Exception e)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var evt = new CommandFailed
                    {
                        CommandId = message.Id,
                        Message = e.Message,
                    };
                    _bus.Publish(evt);
                    MarkAsComplete();
                }
                throw;
            }
        }

        protected abstract void HandleBaseStartCommand(TCommand command);
    }
}
