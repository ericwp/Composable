#region usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using AutoMapper.Mappers;
using Composable.System.IO;
using Composable.System.Reflection;
using Composable.System.Linq;
using Composable.System;

#endregion

namespace Composable.AutoMapper
{
    public static class ComposableMapper
    {
        public static void Init(Func<IMappingEngine> engineProvider)
        {
            _engineProvider = engineProvider;
        }

        private static readonly object LockObject = new Object();
        private static Func<IMappingEngine> _engineProvider;
        private static IMappingEngine Engine
        {
            get
            {
                if (_engineProvider == null)
                {
                    lock (LockObject)
                    {
                        if (_engineProvider == null)
                        {
                            _engineProvider = CreateDefaultProvider();
                        }
                    }
                }
                return _engineProvider();
            }
        }

        private static Func<IMappingEngine> CreateDefaultProvider()
        {
            var configuration = new Configuration(new TypeMapFactory(), MapperRegistry.AllMappers());
            var safeConfiguration = new SafeConfiguration(configuration);

            //todo:hmmmm....
            AppDomain.CurrentDomain.BaseDirectory.AsDirectory().GetFilesResursive().WithExtension(".dll", ".exe")
                .Where(assemblyFile => !assemblyFile.Name.StartsWith("System.", "Microsoft."))
                .Select(assemblyFile => Assembly.LoadFrom(assemblyFile.FullName))
                .SelectMany(GetTypesSafely)
                .Where(t => t.Implements<IProvidesMappings>() && !t.IsInterface && !t.IsAbstract)
                .Select(type => (IProvidesMappings)Activator.CreateInstance(type))
                .ForEach(mappingCreator => mappingCreator.CreateMappings(safeConfiguration));

            configuration.AssertConfigurationIsValid();

            var engine = new MappingEngine(configuration);

            return () => engine;
        }

        private static Type[] GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (Exception e)
            {
                //fixme: Swallowing exceptions is not that great....
                return new Type[0];
            }
        }

        public static TTarget MapTo<TTarget>(this object me)
        {
            return (TTarget) Engine.Map(me, me.GetType(), typeof (TTarget));
        }

        public static IEnumerable<TTarget> MapTo<TTarget>(this IEnumerable me)
        {
            return me.Cast<object>().Select(MapTo<TTarget>);
        }

        public static object MapTo(this object me, Type targetType)
        {
            return Engine.Map(me, me.GetType(), targetType);
        }

        public static IEnumerable<object> MapTo(this IEnumerable me, Type targetType)
        {
            return me.Cast<object>().Select(obj => obj.MapTo(targetType));
        }
    }
}