namespace Composable.Messaging.Buses
{
  using global::System.Collections.Generic;

  public interface IMessageSpy
    {
        IEnumerable<IMessage> DispatchedMessages { get; }
    }
}
