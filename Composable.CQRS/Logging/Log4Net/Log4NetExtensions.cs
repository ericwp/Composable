﻿using log4net;

namespace Composable.Logging.Log4Net
{
    static class Log4NetExtensions
    {
        internal static ILog Log<T>(this T me)
        {
            return LogHolder<T>.Logger;
        }

        static class LogHolder<T>
        {
            // ReSharper disable once StaticFieldInGenericType
            public static readonly ILog Logger = LogManager.GetLogger(typeof(T));
        }
    }
}