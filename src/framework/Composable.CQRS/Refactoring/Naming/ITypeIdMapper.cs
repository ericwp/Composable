using System;
using System.Collections.Generic;

namespace Composable.Refactoring.Naming
{
    interface ITypeIdMapper
    {
        TypeId GetId(Type type);
        Type GetType(TypeId eventTypeId);
        bool TryGetType(TypeId typeId, out Type type);
        void AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings);
    }

    public interface ITypeMappingRegistar
    {
        ITypeMappingRegistar Map<TType>(Guid typeGuid);
        ITypeMappingRegistar Map<TType>(string typeGuid);
    }
}