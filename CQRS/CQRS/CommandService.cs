#region usings

using System;
using System.Linq;
using System.Transactions;
using Composable.System;
using Composable.System.Linq;
using Microsoft.Practices.ServiceLocation;

#endregion

namespace Composable.CQRS
{
    public class CommandService : ICommandService
    {
        private readonly IServiceLocator _serviceLocator;

        public CommandService(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public virtual void Execute<TCommand>(TCommand command) where TCommand : IDomainCommand
        {
            using (var transaction = new TransactionScope())
            {
                if (command is IEntityCommand)
                {
                    var handlerLocators = _serviceLocator.GetAllInstances<IEntityCommandHandlerProvider>();
                    if (handlerLocators.Count() > 1)
                    {
                        throw new Exception("More than one registered ICommandHandlerProvider");
                    }
                    if (handlerLocators.None())
                    {
                        throw new Exception("No registered ICommandHandlerProvider");
                    }

                    var handlerLocator = handlerLocators.Single();
                    var handler = handlerLocator.Provide(command);
                    if (handler == null)
                    {
                        throw new Exception("{0} instance returned null looking for {1}".FormatWith(handlerLocator.GetType(), typeof(TCommand)));
                    }

                    handler.Execute(command);
                }
                else
                {
                    var handlers = _serviceLocator.GetAllInstances<ICommandHandler<TCommand>>();
                    if (handlers.Count() > 1)
                    {
                        throw new Exception(
                            "More than one registered handler for command: {0}".FormatWith(typeof (TCommand)));
                    }
                    if (handlers.None())
                    {
                        throw new Exception("No handler registered for {0}".FormatWith(typeof (TCommand)));
                    }
                    handlers.Single().Execute(command);
                }
                transaction.Complete();
            }
        }
    }
}