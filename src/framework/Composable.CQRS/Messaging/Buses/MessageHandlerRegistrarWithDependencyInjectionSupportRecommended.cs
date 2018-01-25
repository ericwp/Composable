using System;
using Composable.DependencyInjection;
using Composable.System.Reflection;

namespace Composable.Messaging.Buses
{
    public static class MessageHandlerRegistrarWithDependencyInjectionSupportRecommended
    {
        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommand<TCommand>(this MessageHandlerRegistrarWithDependencyInjectionSupport @this, Action<TCommand, ILocalServiceBusSession> action) where TCommand : IExactlyOnceCommand
        {
            @this.Register.ForCommand<TCommand>(command =>  action(command, @this.Resolve<ILocalServiceBusSession>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommandWithResultMine<TCommand, TResult>(this MessageHandlerRegistrarWithDependencyInjectionSupport @this, Func<TCommand, ILocalServiceBusSession, TResult> action) where TCommand : IExactlyOnceCommand<TResult>
        {
            @this.Register.ForCommand<TCommand, TResult>(command =>  action(command, @this.Resolve<ILocalServiceBusSession>()));
            return @this;
        }

      public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent>(this MessageHandlerRegistrarWithDependencyInjectionSupport @this, Action<TEvent, ILocalServiceBusSession> action) where TEvent : IExactlyOnceEvent
        {
            @this.ForEvent<TEvent>(@event => action(@event, @this.Resolve<ILocalServiceBusSession>()));
            return @this;
        }

      public static MessageHandlerRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TResult>(this MessageHandlerRegistrarWithDependencyInjectionSupport @this, Func<TQuery, ILocalServiceBusSession, TResult> action) where TQuery : IQuery<TResult>
        {
            @this.Register.ForQuery<TQuery, TResult>(query => action(query, @this.Resolve<ILocalServiceBusSession>()));
            return @this;
        }
    }
}
