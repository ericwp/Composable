﻿using System;

namespace Composable.Logging
{
    class ConsoleLogger : ILogger
    {
        readonly Type _type;

        ConsoleLogger(Type type) => _type = type;

        public static ILogger Create(Type type) => new ConsoleLogger(type);
        public void Error(Exception exception, string message) => SafeConsole.WriteLine($"ERROR:{_type}: {message} {exception}");
        public void Error(string message) => SafeConsole.WriteLine($"ERROR:{_type}: {message}");
        public void Warning(string message) => SafeConsole.WriteLine($"WARNING:{_type}: {message}");
        public void Info(string message) => SafeConsole.WriteLine($"INFO:{_type}: {message}");
        public void Debug(string message) => SafeConsole.WriteLine($"DEBUG:{_type}: {message}");
    }
}
