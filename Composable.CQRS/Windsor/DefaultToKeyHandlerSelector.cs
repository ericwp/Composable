﻿using System;
using System.Linq;
using Castle.MicroKernel;

namespace Composable.CQRS.Windsor
{
    /// <summary>
    /// When multiple registrations to the same type are made this HandlerSelector defaults selection to the one with the specified key
    /// (instead of the default Windsor behavior of defaulting to the first registered service)
    /// Use it by adding it to the container at wire-up with container.Kernel.AddHandlerSelector(new DefaultToKeyHandlerSelector(typeof([ComponentType]),"defaultKey"));
    /// </summary>
    sealed class DefaultToKeyHandlerSelector : IHandlerSelector
    {
        readonly Type _type;
        readonly string _keyToDefaultTo;

        public DefaultToKeyHandlerSelector(Type type, string keyToDefaultTo)
        {
            _type = type;
            _keyToDefaultTo = keyToDefaultTo;
        }

        public bool HasOpinionAbout(string key, Type service) => service == _type;

        public IHandler SelectHandler(string key, Type service, IHandler[] handlers)
        {
            var handlerForDefaultKey = handlers.FirstOrDefault(handler => handler.ComponentModel.Name == _keyToDefaultTo);
            if (handlerForDefaultKey == null)
                return handlers.FirstOrDefault();

            return handlerForDefaultKey;
        }
    }
}