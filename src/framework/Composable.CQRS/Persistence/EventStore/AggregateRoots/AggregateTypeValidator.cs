﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Composable.System;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.AggregateRoots
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class AllowPublicSettersAttribute : Attribute
    {
    }

    static class AggregateTypeValidator<TDomainClass, TEventClass, TEventInterface>
    {
        public static void Validate()
        {
            List<Type> typesToInspect = Seq.OfTypes<TDomainClass, TEventClass, TEventInterface>().ToList();

            typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TDomainClass)));
            typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TEventClass)));
            typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TEventInterface)));

            typesToInspect = typesToInspect.Distinct().ToList();

            var illegalMembers = typesToInspect.SelectMany(GetBrokenMembers).Distinct().ToList();

            if (illegalMembers.Any())
            {
                // ReSharper disable once PossibleNullReferenceException
                var brokenMembers = illegalMembers.Select(illegal => $"{illegal.DeclaringType.FullName}.{illegal.Name}").Distinct().OrderBy(me => me).Join(Environment.NewLine);
                var message = $@"Types used by aggregate contains types that have public setters or public  fields. This is a dangerous design. 
If you ever mutate an event or an aggregate except by raising events your state is likely to become currupt in our caches etc. 
List of problem members:{Environment.NewLine}{brokenMembers}{Environment.NewLine}{Environment.NewLine}";

                Console.WriteLine(message);

                throw new Exception(message);
            }
        }

        static IEnumerable<MemberInfo> GetBrokenMembers(Type type)
        {
            var publicFields = type.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(member => member.MemberType.HasFlag(MemberTypes.Field)).ToList();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var publicProperties = properties
                .Where(member => member?.SetMethod?.IsPublic == true)
                .ToList();

            var totalMutableProperties = publicFields.Concat(publicProperties).ToList();
            // ReSharper disable once AssignNullToNotNullAttribute
            totalMutableProperties = totalMutableProperties.Where(member => member.DeclaringType.GetCustomAttribute<AllowPublicSettersAttribute>() == null).ToList();

            return totalMutableProperties;
        }


        static IReadOnlyList<Type> GetAllInheritingClassesOrInterfaces(Type type) => type.Assembly.GetTypes()
                                                                                       .Where(type.IsAssignableFrom)
                                                                                       .ToList();
    }
}