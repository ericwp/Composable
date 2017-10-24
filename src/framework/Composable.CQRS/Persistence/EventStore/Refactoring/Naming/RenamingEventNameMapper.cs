using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Messaging;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Persistence.EventStore.Refactoring.Naming
{
    class RenamingEventNameMapper : IEventNameMapper
    {
        public string GetName(Type eventType)
        {
            if (_typeToNameMappings.TryGetValue(eventType, out string name))
            {
                return name;
            }

            throw new Exception($"Failed to find event name for: {eventType}.");
        }

        public Type GetType(string eventTypeName)
        {
            if (_nameToTypeMappings.TryGetValue(eventTypeName, out Type type))
            {
                return type;
            }

            throw new CouldNotFindTypeBasedOnName(eventTypeName);
        }

        public RenamingEventNameMapper(IEnumerable<Type> eventTypes, params IRenameEvents[] renamers)
        {
            var nameMappings = eventTypes
                .Where(type => type.Implements<IDomainEvent>())
                .Select(type => new EventNameMapping(type))
                .ToArray();

            nameMappings.ForEach(mapping => renamers.ForEach(renamer => renamer.Rename(mapping)));

            AssertMappingsAreValid(nameMappings);

            _nameToTypeMappings = nameMappings.ToDictionary(
                keySelector: mapping => mapping.FullName,
                elementSelector: mapping => mapping.Type
                );

            _typeToNameMappings = nameMappings.ToDictionary(
                keySelector: mapping => mapping.Type,
                elementSelector: mapping => mapping.FullName
                );
        }

        readonly Dictionary<string, Type> _nameToTypeMappings;
        readonly Dictionary<Type, string> _typeToNameMappings;

        static void AssertMappingsAreValid(EventNameMapping[] mappings)
        {
            var detectedDuplicate = mappings.GroupBy(mapping => mapping.FullName)
                                            .Where(grouping => grouping.Count() > 1)
                                            .FirstOrDefault();

            if(detectedDuplicate != null)
            {
                throw new Exception(
                    $@"Duplicate event name detected: 
Name: {detectedDuplicate.Key}
Claimed by:
    {
                        detectedDuplicate.ToArray().Select(mapping => mapping.Type.FullName).Join($"{Environment.NewLine}    ")}");
            }
        }
    }
}
