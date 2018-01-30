using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.System.Reflection;
using Newtonsoft.Json;

namespace Composable.Serialization
{

    class RenamingSupportingJsonSerializer : IJsonSerializer
    {
        readonly JsonSerializerSettings _jsonSettings;
        readonly RenamingDecorator _renamingDecorator;
        protected internal RenamingSupportingJsonSerializer(JsonSerializerSettings jsonSettings, TypeMapper typeMapper)
        {
            _jsonSettings = jsonSettings;
            _renamingDecorator = new RenamingDecorator(typeMapper);
        }

        public string Serialize(object instance)
        {
            var json = JsonConvert.SerializeObject(instance, Formatting.Indented, _jsonSettings);
            json = _renamingDecorator.ReplaceTypeNames(json);
            return json;
        }

        public object Deserialize(Type eventType, string json)
        {
            json = _renamingDecorator.RestoreTypeNames(json);
            return JsonConvert.DeserializeObject(json, eventType, _jsonSettings);
        }
    }

    class RenamingDecorator
    {
        readonly TypeMapper _typeMapper;

        static readonly Regex  FindTypeNames = new Regex(@"""\$type""\: ""([^""]*)""", RegexOptions.Compiled);
        public RenamingDecorator(TypeMapper typeMapper) => _typeMapper = typeMapper;

        public string ReplaceTypeNames(string json) => FindTypeNames.Replace(json, ReplaceTypeNamesWithTypeIds);

        string ReplaceTypeNamesWithTypeIds(Match match)
        {
            var type = Type.GetType(match.Groups[1].Value);
            var typeId = _typeMapper.GetId(type);
            return $@"""$type"": ""{typeId.GuidValue}""";
        }

        public string RestoreTypeNames(string json) => FindTypeNames.Replace(json, ReplaceTypeIdsWithTypeNames);

        string ReplaceTypeIdsWithTypeNames(Match match)
        {
            var typeId = new TypeId(Guid.Parse(match.Groups[1].Value));
            var type = _typeMapper.GetType(typeId);
            return $@"""$type"": ""{type.AssemblyQualifiedName}""";
        }
    }


    class NewtonSoftEventStoreSerializer : IEventStoreSerializer
    {
        internal static readonly JsonSerializerSettings JsonSettings = Serialization.JsonSettings.SqlEventStoreSerializerSettings;

        readonly RenamingSupportingJsonSerializer _serializer;

        public NewtonSoftEventStoreSerializer(TypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings, typeMapper);

        public string Serialize(object @event) => _serializer.Serialize(@event);
        public IAggregateEvent Deserialize(Type eventType, string json) => (IAggregateEvent)_serializer.Deserialize(eventType, json);
    }

    class NewtonDocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
    {
        public NewtonDocumentDbSerializer(TypeMapper typeMapper) : base(JsonSettings.SqlEventStoreSerializerSettings, typeMapper) {}
    }

    class NewtonBusMessageSerializer : RenamingSupportingJsonSerializer, IBusMessageSerializer
    {
        public NewtonBusMessageSerializer(TypeMapper typeMapper) : base(JsonSettings.SqlEventStoreSerializerSettings, typeMapper) {}
    }
}