using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Composable.CQRS.Command
{
    public abstract class CompositeCommand : Command
    {
        public abstract IEnumerable<SubCommand> GetContainedCommands();

        protected CompositeCommand() : this(Guid.NewGuid())
        {
        }

        protected CompositeCommand(Guid id) : base(id)
        {
        }
    }
}