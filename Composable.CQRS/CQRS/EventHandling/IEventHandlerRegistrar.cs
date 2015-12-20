﻿using System;

namespace Composable.CQRS.EventHandling
{
    public interface IEventHandlerRegistrar<in TBaseEvent>
        where TBaseEvent : class
    {
        ///<summary>Registers a handler for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
        IEventHandlerRegistrar<TBaseEvent> For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TBaseEvent;

        ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
        /// Useful for listening to generic events such as IAggregateRootCreatedEvent or IAggregateRootDeletedEvent
        /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
        /// </summary>
        IEventHandlerRegistrar<TBaseEvent> ForGenericEvent<THandledEvent>(Action<THandledEvent> handler);

        IEventHandlerRegistrar<TBaseEvent> BeforeHandlers<THandledEvent>(Action<THandledEvent> runBeforeHandlers) where THandledEvent : TBaseEvent;
        IEventHandlerRegistrar<TBaseEvent> AfterHandlers<THandledEvent>(Action<THandledEvent> runAfterHandlers) where THandledEvent : TBaseEvent;
        IEventHandlerRegistrar<TBaseEvent> IgnoreUnhandled<T>();
    }

    public static class EventHandlerRegistrar
    {
        public static IEventHandlerRegistrar<TBaseEvent> BeforeHandlers<TBaseEvent>
            (this IEventHandlerRegistrar<TBaseEvent> @this, Action<TBaseEvent> handler) where TBaseEvent : class
        {
            return @this.BeforeHandlers<TBaseEvent>(handler);
        }

        public static IEventHandlerRegistrar<TBaseEvent> AfterHandlers<TBaseEvent>
            (this IEventHandlerRegistrar<TBaseEvent> @this, Action<TBaseEvent> handler) where TBaseEvent : class
        {
            return @this.AfterHandlers<TBaseEvent>(handler);
        }        
    }
}
