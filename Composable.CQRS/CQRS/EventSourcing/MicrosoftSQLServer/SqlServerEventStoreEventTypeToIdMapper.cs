using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Composable.CQRS.EventSourcing.Refactoring.Naming;
using Composable.Logging.Log4Net;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    internal class SqlServerEventStoreEventTypeToIdMapper : IEventTypeToIdMapper
    {
        private readonly IEventNameMapper _nameMapper;

        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        public SqlServerEventStoreEventTypeToIdMapper(string connectionString, IEventNameMapper nameMapper)
        {
            _nameMapper = nameMapper;
            _connectionMananger = new SqlServerEventStoreConnectionManager(connectionString);
        }

        public Type GetType(int id)
        {            
            lock(_lockObject)
            {
                EnsureInitialized();
                IIdTypeMapping result;
                if(_idToTypeMap.TryGetValue(id, out result))
                {
                    return result.Type;
                }

                LoadTypesFromDatabase();

                if (!_idToTypeMap.TryGetValue(id, out result))
                {
                    throw new Exception($"Failed to load type information Id: {id} from the eventstore");
                }

                return result.Type;
            }
        }        

        public int GetId(Type type)
        {
            lock(_lockObject)
            {
                EnsureInitialized();
                int value;
                if(!_typeToIdMap.TryGetValue(type, out value))
                {
                    var mapping = InsertNewType(type);
                    _idToTypeMap.Add(mapping.Id, mapping);
                    _typeToIdMap.Add(mapping.Type, mapping.Id);
                    value = mapping.Id;
                }
                return value;
            }
        }

        private void EnsureInitialized()
        {
            if (_idToTypeMap == null)
            {
                LoadTypesFromDatabase();
            }
        }
        private void LoadTypesFromDatabase()
        {
            lock(_lockObject)
            {
                var idToTypeMap = new Dictionary<int, IIdTypeMapping>();
                var typeToIdMap = new Dictionary<Type, int>();
                foreach(var mapping in GetTypes())
                {
                    idToTypeMap.Add(mapping.Id, mapping);
                    if(!(mapping is BrokenIdTypeMapping))
                    {
                        typeToIdMap.Add(mapping.Type, mapping.Id);
                    }
                }
                _idToTypeMap = idToTypeMap;
                //Only assign to the fields once we completely and successfully fetch all types. We do not want a half-way populated, and therefore corrupt, mapping table.
                _typeToIdMap = typeToIdMap;
            }
        }

        private IdTypeMapping InsertNewType(Type newType)
        {          
            using (var connection = _connectionMananger.OpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"INSERT {EventTypeTable.Name} ( {EventTypeTable.Columns.EventType} ) OUTPUT INSERTED.{EventTypeTable.Columns.Id} VALUES( @{EventTypeTable.Columns.EventType} )";
                    command.Parameters.Add(new SqlParameter(EventTypeTable.Columns.EventType, _nameMapper.GetName(newType)));
                    return new IdTypeMapping(id: (int)command.ExecuteScalar(), type: newType);
                }
            }
        }       

        private IEnumerable<IIdTypeMapping> GetTypes()
        {
            using(var connection = _connectionMananger.OpenConnection())
            {
                using(var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT {EventTypeTable.Columns.Id} , {EventTypeTable.Columns.EventType} FROM {EventTypeTable.Name}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var eventTypeName = reader.GetString(1);
                            var eventTypeId = reader.GetInt32(0);
                            Type foundEventType = null;

                            try
                            {
                                foundEventType = _nameMapper.GetType(eventTypeName);
                            }
                            catch (CouldNotFindTypeBasedOnName)
                            {
                                this.Log().Warn($"The type of event: Id: {eventTypeId}, Name: {eventTypeName} that exists in the database could not be found in the loaded assemblies. No mapping will be created for this class. If an event of this type is read from the store an exception will be thrown");
                            }

                            if(foundEventType != null)
                            {
                                yield return new IdTypeMapping(id: eventTypeId, type: foundEventType);
                            }
                            else
                            {
                                yield return new BrokenIdTypeMapping(id: eventTypeId, typeName: eventTypeName);
                            }
                        }
                    }
                }
            }
        }


        private Dictionary<int, IIdTypeMapping> _idToTypeMap;
        private Dictionary<Type, int> _typeToIdMap;
        private readonly object _lockObject = new object();

        private interface IIdTypeMapping {
            int Id { get; }
            Type Type { get; }
        }

        private class BrokenIdTypeMapping : IIdTypeMapping
        {
            private readonly string _typeName;
            public BrokenIdTypeMapping(int id, string typeName)
            {
                _typeName = typeName;
                Id = id;
            }
            public int Id { get; }
            public Type Type { get { throw new TryingToReadEventOfTypeThatNoMappingCouldBeFoundForException(_typeName, Id);} }
        }

        private class IdTypeMapping : IIdTypeMapping
        {
            public int Id { get; }
            public Type Type { get; }
            public IdTypeMapping(int id, Type type)
            {
                Id = id;
                Type = type;
            }
        }
    }

    public class TryingToReadEventOfTypeThatNoMappingCouldBeFoundForException : Exception
    {
        public TryingToReadEventOfTypeThatNoMappingCouldBeFoundForException(string typeName, int id):base($"Event type Id: {id}, Name: {typeName} could not be mapped to a type.") {  }
    }
}