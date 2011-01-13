#region usings

using System.Transactions;
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

        public virtual void Execute<TCommand>(TCommand command)
        {
            using (var transaction = new TransactionScope())
            {
                var handler = _serviceLocator.GetInstance<ICommandHandler<TCommand>>();                
                handler.Execute(command);
                transaction.Complete();
            }
        }
    }
}