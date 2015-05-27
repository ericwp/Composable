﻿using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
using Composable.System.Linq;
using JetBrains.Annotations;
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary>
    /// Sends/Publishes messages to <see cref="IHandleMessages{T}"/> implementations registered in the <see cref="IWindsorContainer"/>.
    /// </summary>
    [UsedImplicitly]
    public partial class SynchronousBus : IServiceBus
    {
        private readonly IWindsorContainer _container;
        private readonly MessageHandlersResolver _handlersResolver;

        public SynchronousBus(IWindsorContainer container)
        {
            _container = container;
            _handlersResolver = new MessageHandlersResolver(container: container,
                handlerInterfaces: new[] { typeof(IHandleInProcessMessages<>), typeof(IHandleMessages<>) },
                excludedHandlerInterfaces: new[] { typeof(IHandleRemoteMessages<>) });
        }

        public virtual void Publish(object message)
        {
            PublishLocal(message);
        }

        public virtual bool Handles(object message)
        {
            return _handlersResolver.HasHandlerFor(message);
        }

        protected virtual void PublishLocal(object message)
        {
            using (_container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using (var transactionalScope = _container.BeginTransactionalUnitOfWorkScope())
                {
                    var handlers = _handlersResolver.GetHandlers(message).ToArray();
                    try
                    {
                        foreach (var messageHandlerReference in handlers)
                        {
                            messageHandlerReference.InvokeHandlers(message);
                        }
                        transactionalScope.Commit();
                    }
                    finally
                    {
                        handlers.ForEach(_container.Release);
                    }
                }
            }
        }

        protected virtual void SyncSendLocal(object message)
        {
            // TODO: Same as PublishLocal, try to remove repeated code.
            using (_container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using (var transactionalScope = _container.BeginTransactionalUnitOfWorkScope())
                {
                    var handlers = _handlersResolver.GetHandlers(message).ToArray();
                    try
                    {
                        AssertThatThereIsExactlyOneRegisteredHandler(handlers, message);

                        foreach (var messageHandlerReference in handlers)
                        {
                            messageHandlerReference.InvokeHandlers(message);
                        }
                        transactionalScope.Commit();
                    }
                    finally
                    {
                        handlers.ForEach(_container.Release);
                    }
                }
            }
        }

        public virtual void SendLocal(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Send(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Reply(object message)
        {
            SyncSendLocal(message);
        }


        private static void AssertThatThereIsExactlyOneRegisteredHandler(MessageHandlersResolver.MessageHandlerReference[] handlers, object message)
        {
            if (handlers.Length == 0)
            {
                throw new NoHandlerException(message.GetType());
            }
            if (handlers.Length > 1)
            {
                //TODO: Maybe we can avoid these code.
                var realHandlers = handlers.Select(handler => handler.Instance)
                    .Where(handler => !(handler is ISynchronousBusMessageSpy))
                    .ToList();
                if (realHandlers.Count > 1)
                {
                    throw new MultipleMessageHandlersRegisteredException(message, realHandlers);
                }
            }
        }
    }
}
