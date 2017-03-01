namespace Composable.Messaging.Commands
{
  using Composable.System.Linq;

  using global::System;
  using global::System.Linq.Expressions;

  public class SubCommand : ISubCommand
    {
        Func<Command> _accessor;
        
        public string Name { get; private set; }
        
        public Command Command { get { return _accessor();  } }

        public SubCommand(Expression<Func<Command>> commandToFind)
        {
            Name = ExpressionUtil.ExtractMemberName(commandToFind);
            _accessor = commandToFind.Compile();
        }
    }
}