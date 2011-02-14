﻿using System;
using Composable.System;

namespace Composable.CQRS
{
    public class NoRegisteredHandlersException : Exception
    {
        public NoRegisteredHandlersException(Type requestedType):base("No handlers registered for {0}:".FormatWith(requestedType))
        {
        }
    }
}