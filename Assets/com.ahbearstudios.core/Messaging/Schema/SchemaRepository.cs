using System;
using System.Collections.Generic;
using System.IO;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using MongoDB.Bson;
using UnityEngine;

namespace AhBearStudios.Core.Messaging.Schema
{
    /// <summary>
    /// Utility for exporting and importing message schemas
    /// </summary>
    public class SchemaRepository
    {
        private readonly IMessageSchemaGenerator _schemaGenerator;
        private readonly Dictionary<string, MessageSchema> _schemas = new Dictionary<string, MessageSchema>();
        private readonly IBurstLogger _logger;
        
        public SchemaRepository(IMessageSchemaGenerator schemaGenerator, IBurstLogger logger = null)
        {
            _schemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
            _logger = logger;
        }
        
        /// <summary>
        /// Adds a schema to the repository
        /// </summary>
        /// <param name="schema">The schema to add</param>
        public void AddSchema(MessageSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
                
            _schemas[schema.Name] = schema;
        }
        
        /// <summary>
        /// Gets a schema by name
        /// </summary>
        /// <param name="name">The name of the schema</param>
        /// <returns>The schema, or null if not found</returns>
        public MessageSchema GetSchema(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
                
            return _schemas.TryGetValue(name, out var schema) ? schema : null;
        }
        
        /// <summary>
        /// Gets all schemas in the repository
        /// </summary>
        /// <returns>The schemas</returns>
        public IEnumerable<MessageSchema> GetAllSchemas()
        {
            return _schemas.Values;
        }
        
        /// <summary>
        /// Exports schemas to the specified directory
        /// </summary>
        /// <param name="directory">The directory to export to</param>
        public void ExportSchemas(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
                
            Directory.CreateDirectory(directory);
            
            foreach (var schema in _schemas.Values)
            {
                var filePath = Path.Combine(directory, $"{schema.Name}.schema.json");
                var json = schema.ToJson();
                File.WriteAllText(filePath, json);
                
                _logger?.Log(LogLevel.Debug, $"Exported schema {schema.Name} to {filePath}", "Serialization");
            }
        }
        
        /// <summary>
        /// Imports schemas from the specified directory
        /// </summary>
        /// <param name="directory">The directory to import from</param>
        public void ImportSchemas(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
                
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Directory not found: {directory}");
                
            var files = Directory.GetFiles(directory, "*.schema.json");
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var schema = MessageSchema.FromJson(json);
                    
                    _schemas[schema.Name] = schema;
                    
                    _logger?.Log(LogLevel.Debug, $"Imported schema {schema.Name} from {file}", "Serialization");
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, $"Error importing schema from {file}: {ex.Message}", "Serialization");
                }
            }
        }
        
        /// <summary>
        /// Generates schemas for all message types in the specified assemblies
        /// </summary>
        /// <param name="messageTypeDiscovery">The message type discovery service</param>
        public void GenerateSchemas(IMessageTypeDiscovery messageTypeDiscovery)
        {
            if (messageTypeDiscovery == null)
                throw new ArgumentNullException(nameof(messageTypeDiscovery));
                
            var messageTypes = messageTypeDiscovery.DiscoverMessageTypes();
            
            foreach (var messageType in messageTypes)
            {
                try
                {
                    var schema = _schemaGenerator.GenerateSchema(messageType);
                    _schemas[schema.Name] = schema;
                    
                    _logger?.Log(LogLevel.Debug, $"Generated schema for {messageType.Name}", "Serialization");
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, $"Error generating schema for {messageType.Name}: {ex.Message}", "Serialization");
                }
            }
        }
    }
}