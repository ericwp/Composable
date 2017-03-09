﻿using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    [UsedImplicitly] public class InProcessServiceBus : IInProcessServiceBus
    {
        readonly IMessageHandlerRegistry _handlerRegistry;

        public InProcessServiceBus(IMessageHandlerRegistry handlerRegistry) { _handlerRegistry = handlerRegistry; }

        void IInProcessServiceBus.Publish(IEvent anEvent)
        {
            _handlerRegistry.CreateEventDispatcher()
                            .Dispatch(anEvent);
            AfterDispatchingMessage(anEvent);
        }

        void IInProcessServiceBus.Send(ICommand message)
        {
            _handlerRegistry.GetCommandHandler(message)(message);
            AfterDispatchingMessage(message);
        }

        TResult IInProcessServiceBus.Get<TResult>(IQuery<TResult> query)
        {
            var returnValue = _handlerRegistry.GetQueryHandler(query)
                                              .Invoke(query);
            AfterDispatchingMessage(query);
            AfterDispatchingMessage(returnValue);
            return returnValue;
        }

        public bool Handles(object aMessage) { return _handlerRegistry.Handles(aMessage); }

        protected virtual void AfterDispatchingMessage(IMessage message) { }
    }
}
