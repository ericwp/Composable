﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NServiceBus;

namespace Composable.ServiceBus
{
    public class SynchronousBusHandlerRegistry
    {
        private static readonly Dictionary<Type, List<MessageHandler>> HandlerToMessageHandlersMap = new Dictionary<Type, List<MessageHandler>>();
        public static IEnumerable<Action<object, object>> Register<TMessage>(object handler, TMessage message)
        {
            List<MessageHandler> messageHandleHolders;
            var handlerType = handler.GetType();
            if (!HandlerToMessageHandlersMap.TryGetValue(handlerType, out messageHandleHolders))
            {
                HandlerToMessageHandlersMap[handlerType] = GetIHandleMessageImplementations(handler.GetType());
            }

            var methodList = HandlerToMessageHandlersMap[handlerType]
                .Where(messageHandler => messageHandler.HandledMessageType.IsInstanceOfType(message))
                .Select(holder => holder.HandlerMethod);
            return methodList;
        }

        //Creates a list of handlers. One per implementation of IHandleMessages in the handlerType
        private static List<MessageHandler> GetIHandleMessageImplementations(Type handlerType)
        {
            var holders = new List<MessageHandler>();

            var handledMessageTypes = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .Select(i => i.GetGenericArguments().First())
                .ToList();

            handledMessageTypes.ForEach(messageType =>
                                 {
                                     var action = TryGetImplementingMethod(handlerType, messageType);
                                     if (action != null)
                                     {

                                         holders.Add(new MessageHandler(messageType, action));
                                     }

                                 });
            return holders;
        }

        //If messageHandlerType implements IHandleMessages<MessageType> then returns an action that can be used to invoke this implementation for a given handler instance.
        private static Action<object, object> TryGetImplementingMethod(Type messageHandlerType, Type messageType)
        {
            var interfaceType = typeof(IHandleMessages<>).MakeGenericType(messageType);
            if(!interfaceType.IsAssignableFrom(messageHandlerType))
            {
                return null;
            }

            var methodInfo = messageHandlerType.GetInterfaceMap(interfaceType).TargetMethods.First();
            //return OptimizeMethodCall(messageHandlerType, methodInfo); If we prove that using invoke is too slow switch to this line instead. If it is not a problem remove this permanently.
            return (handler, message) => methodInfo.Invoke(handler, new []{ message });
        }

        private static Action<object, object> OptimizeMethodCall_remove_me_unless_big_performance_advantages_are_proven_to_exist(Type messageHandlerType, MethodInfo methodInfo)
        {
            var target = Expression.Parameter(typeof(object));
            var param = Expression.Parameter(typeof(object));

            var castTarget = Expression.Convert(target, messageHandlerType);
            var castParam = Expression.Convert(param, methodInfo.GetParameters().First().ParameterType);
            var execute = Expression.Call(castTarget, methodInfo, castParam);
            return Expression.Lambda<Action<object, object>>(execute, target, param).Compile();
        }
    }

    ///<summary>Used to hold a single implementation of IHandleMessages</summary>
    internal class MessageHandler
    {
        public Type HandledMessageType { get; private set; }
        public Action<object, object> HandlerMethod { get; private set; }

        public MessageHandler(Type handledMessageType, Action<object, object> handlerMethod)
        {
            HandledMessageType = handledMessageType;
            HandlerMethod = handlerMethod;
        }
    }
}
