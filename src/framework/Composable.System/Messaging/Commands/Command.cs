using System;
using Composable.DDD;

namespace Composable.Messaging.Commands
{
    public class Command : ValueObject<Command>, ICommand
    {
        public Guid Id { get; private set; }

        protected Command()
            : this(Guid.NewGuid()) { }

        Command(Guid id) => Id = id;
    }
}
