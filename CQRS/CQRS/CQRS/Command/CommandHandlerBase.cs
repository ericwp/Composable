﻿using System;
using System.Transactions;
using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.Command
{
    public abstract class CommandHandlerBase<TCommand, TCommandSuccess, TCommandFailed> : IHandleMessages<TCommand>
        where TCommand : Command
        where TCommandSuccess : CommandSuccess
        where TCommandFailed : CommandFailed, new()
    {
        private readonly IServiceBus _bus;

        protected CommandHandlerBase(IServiceBus bus)
        {
            _bus = bus;
        }

        public void Handle(TCommand message)
        {
            try
            {
                var commandSuccess = HandleCommand(message);
                using(new TransactionScope(TransactionScopeOption.Suppress))
                {
                    _bus.Publish(commandSuccess);
                }
            }
            catch (Exception e)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var commandFailed = CreateCommandFailedException(e, message);
                    _bus.Publish(commandFailed);
                }
                //Always throw uncaught Exceptions so that surrounding infrastructure can handle it
                throw;
            }
        }

        protected virtual TCommandFailed CreateCommandFailedException(Exception e, TCommand message)
        {
            return new TCommandFailed
            {
                CommandId = message.Id,
                Message = e.Message,
            };
        }
        protected abstract TCommandSuccess HandleCommand(TCommand command);
    }


}
