﻿using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.ServiceBus
{
    ///<summary>Resolves message handlers that inherits from <see cref="IHandleMessages{T}"/>.
    /// <remarks>Does not return message handlers that implements <see cref="IHandleRemoteMessages{T}"/>.</remarks>
    /// </summary>
    public class DefaultMessageHandlerResolver : MessageHandlerResolver
    {
        public DefaultMessageHandlerResolver(IWindsorContainer container)
            : base(container) {}

        override public Type HandlerInterfaceType { get { return typeof(IHandleMessages<>); } }

        override public bool HasHandlerFor<TMessage>(TMessage message)
        {
            var handlers = ResolveMessageHandlers(message);
            handlers.ForEach(Container.Release);
            return handlers.Any();
        }

        override public List<object> ResolveMessageHandlers<TMessage>(TMessage message)
        {
            // We don't want to dispatch messages to a IHandleRemoteMessages handler.
            // Get all combination of IHandleRemoteMessages and IMessages.
            var remoteMessageHandlerType = message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(m => m.Implements(typeof(IMessage)))
                .Select(m => typeof(IHandleRemoteMessages<>).MakeGenericType(m));

            return base.ResolveMessageHandlers(message)
                //Make sure not to dispatch message to an IHandleRemoteMessages instance.
                .Where(h =>remoteMessageHandlerType.None(r=>r.IsInstanceOfType(h)))
                .ToList();
        }
    }

    ///<summary>Resolves message handlers that inherits from <see cref="IHandleInProcessMessages{T}"/>.</summary>
    public class InProcessMessageHandlerResolver : MessageHandlerResolver
    {
        public InProcessMessageHandlerResolver(IWindsorContainer container)
            : base(container) {}

        override public Type HandlerInterfaceType { get { return typeof(IHandleInProcessMessages<>); } }
    }

    public abstract class MessageHandlerResolver
    {
        protected readonly IWindsorContainer Container;
        
        public abstract Type HandlerInterfaceType { get; }

        protected MessageHandlerResolver(IWindsorContainer container)
        {
            Container = container;
        }

        public virtual List<object> ResolveMessageHandlers<TMessage>(TMessage message)
        {
            var handlers = new List<object>();
            foreach(var handlerType in GetHandlerTypes(message, HandlerInterfaceType))
            {
                foreach(var handlerInstance in Container.ResolveAll(handlerType).Cast<object>())
                {
                    if(!handlers.Contains(handlerInstance))
                    {
                        handlers.Add(handlerInstance);
                    }
                }
            }

            return handlers;
        }

        public virtual bool HasHandlerFor<TMessage>(TMessage message)
        {
            return GetHandlerTypes(message, HandlerInterfaceType).Any();
        }

        protected IEnumerable<Type> GetHandlerTypes(object message, Type handlerInterfaceType)
        {
            return message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(m => m.Implements(typeof(IMessage)))
                .Select(m => handlerInterfaceType.MakeGenericType(m))
                .Where(i => Container.Kernel.HasComponent(i))
                .ToArray();

        }
    }
}
