using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Attributes;
using AhBearStudios.Core.Messaging.Data;
using AhBearStudios.Core.Messaging.Interfaces;
using Realms;

namespace AhBearStudios.Core.Messaging.Schema
{
    /// <summary>
    /// Implementation of a message schema generator
    /// </summary>
    public class MessageSchemaGenerator : IMessageSchemaGenerator
    {
        private readonly Dictionary<Type, MessageSchema> _schemaCache = new Dictionary<Type, MessageSchema>();
        private readonly IBurstLogger _logger;
        
        public MessageSchemaGenerator(IBurstLogger logger = null)
        {
            _logger = logger;
        }
        
        public MessageSchema GenerateSchema<TMessage>() where TMessage : IMessage
        {
            return GenerateSchema(typeof(TMessage));
        }
        
        public MessageSchema GenerateSchema(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));
                
            if (!typeof(IMessage).IsAssignableFrom(messageType))
                throw new ArgumentException($"Type {messageType.Name} does not implement IMessage", nameof(messageType));
                
            // Check cache first
            if (_schemaCache.TryGetValue(messageType, out var cachedSchema))
            {
                return cachedSchema;
            }
            
            var schema = new MessageSchema(messageType);
            
            // Get type metadata
            var typeAttrs = messageType.GetCustomAttributes(true);
            
            // Set schema metadata from attributes
            foreach (var attr in typeAttrs)
            {
                if (attr is MessageTypeAttribute messageTypeAttr)
                {
                    schema.Description = messageTypeAttr.Description ?? schema.Description;
                    schema.Version = messageTypeAttr.Version;
                }
                
                if (attr is DescriptionAttribute descAttr)
                {
                    schema.Description = descAttr.Description;
                }
                
                if (attr is DisplayAttribute displayAttr)
                {
                    schema.Name = displayAttr.Name ?? schema.Name;
                    schema.Description = displayAttr.Description ?? schema.Description;
                }
            }
            
            // Get properties
            var properties = messageType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                // Skip IMessage properties - they're not part of the schema
                if (property.Name == nameof(IMessage.Id) || property.Name == nameof(IMessage.Timestamp))
                    continue;
                    
                var propSchema = CreatePropertySchema(property);
                schema.Properties.Add(propSchema);
            }
            
            // Get fields
            var fields = messageType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var propSchema = CreatePropertySchema(field);
                schema.Properties.Add(propSchema);
            }
            
            // Sort properties by name for consistency
            schema.Properties.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            
            // Cache the schema
            _schemaCache[messageType] = schema;
            
            return schema;
        }
        
        public SchemaValidationResult ValidateMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            return ValidateMessage(message, typeof(TMessage));
        }
        
        public SchemaValidationResult ValidateMessage(object message, Type messageType)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));
                
            var result = new SchemaValidationResult();
            
            // Get the schema
            var schema = GenerateSchema(messageType);
            
            // Validate each property
            foreach (var propSchema in schema.Properties)
            {
                // Get the property value
                var property = messageType.GetProperty(propSchema.Name);
                
                if (property != null)
                {
                    var value = property.GetValue(message);
                    
                    // Check required properties
                    if (propSchema.IsRequired && value == null)
                    {
                        result.AddError(propSchema.Name, $"Property {propSchema.Name} is required but was null");
                        continue;
                    }
                    
                    // Check constraints
                    foreach (var constraint in propSchema.Constraints)
                    {
                        if (!ValidateConstraint(constraint, value, property.PropertyType))
                        {
                            result.AddError(propSchema.Name, constraint.ErrorMessage ?? $"Property {propSchema.Name} failed validation for constraint {constraint.Type}");
                        }
                    }
                }
                else
                {
                    // Try as a field
                    var field = messageType.GetField(propSchema.Name);
                    
                    if (field != null)
                    {
                        var value = field.GetValue(message);
                        
                        // Check required fields
                        if (propSchema.IsRequired && value == null)
                        {
                            result.AddError(propSchema.Name, $"Field {propSchema.Name} is required but was null");
                            continue;
                        }
                        
                        // Check constraints
                        foreach (var constraint in propSchema.Constraints)
                        {
                            if (!ValidateConstraint(constraint, value, field.FieldType))
                            {
                                result.AddError(propSchema.Name, constraint.ErrorMessage ?? $"Field {propSchema.Name} failed validation for constraint {constraint.Type}");
                            }
                        }
                    }
                    else
                    {
                        // Property/field not found - schema mismatch
                        result.AddError(propSchema.Name, $"Property/field {propSchema.Name} not found on message of type {messageType.Name}");
                    }
                }
            }
            
            return result;
        }
        
        private PropertySchema CreatePropertySchema(PropertyInfo property)
        {
            var propSchema = new PropertySchema
            {
                Name = property.Name,
                TypeName = GetFriendlyTypeName(property.PropertyType)
            };
            
            // Get property metadata from attributes
            var propAttrs = property.GetCustomAttributes(true);
            
            foreach (var attr in propAttrs)
            {
                if (attr is RequiredAttribute)
                {
                    propSchema.IsRequired = true;
                    propSchema.Constraints.Add(new ValidationConstraint("required", "true", $"{property.Name} is required"));
                }
                
                if (attr is RangeAttribute rangeAttr)
                {
                    propSchema.Constraints.Add(new ValidationConstraint("range", $"{rangeAttr.Minimum},{rangeAttr.Maximum}", rangeAttr.ErrorMessage ?? $"{property.Name} must be between {rangeAttr.Minimum} and {rangeAttr.Maximum}"));
                }
                
                if (attr is StringLengthAttribute stringLengthAttr)
                {
                    propSchema.Constraints.Add(new ValidationConstraint("stringLength", $"{stringLengthAttr.MinimumLength},{stringLengthAttr.MaximumLength}", stringLengthAttr.ErrorMessage ?? $"{property.Name} must be between {stringLengthAttr.MinimumLength} and {stringLengthAttr.MaximumLength} characters"));
                }
                
                if (attr is RegularExpressionAttribute regexAttr)
                {
                    propSchema.Constraints.Add(new ValidationConstraint("regex", regexAttr.Pattern, regexAttr.ErrorMessage ?? $"{property.Name} must match pattern {regexAttr.Pattern}"));
                }
                
                if (attr is DescriptionAttribute descAttr)
                {
                    propSchema.Description = descAttr.Description;
                }
                
                if (attr is DefaultValueAttribute defaultValueAttr)
                {
                    propSchema.DefaultValue = defaultValueAttr.Value?.ToString();
                }
                
                if (attr is DisplayAttribute displayAttr)
                {
                    propSchema.Description = displayAttr.Description ?? propSchema.Description;
                }
            }
            
            return propSchema;
        }
        
        private PropertySchema CreatePropertySchema(FieldInfo field)
        {
            var propSchema = new PropertySchema
            {
                Name = field.Name,
                TypeName = GetFriendlyTypeName(field.FieldType)
            };
            
            // Get field metadata from attributes
            var fieldAttrs = field.GetCustomAttributes(true);
            
            foreach (var attr in fieldAttrs)
            {
                if (attr is RequiredAttribute)
                {
                    propSchema.IsRequired = true;
                    propSchema.Constraints.Add(new ValidationConstraint("required", "true", $"{field.Name} is required"));
                }
                
                if (attr is RangeAttribute rangeAttr)
                {
                    propSchema.Constraints.Add(new ValidationConstraint("range", $"{rangeAttr.Minimum},{rangeAttr.Maximum}", rangeAttr.ErrorMessage ?? $"{field.Name} must be between {rangeAttr.Minimum} and {rangeAttr.Maximum}"));
                }
                
                if (attr is StringLengthAttribute stringLengthAttr)
                {
                    propSchema.Constraints.Add(new ValidationConstraint("stringLength", $"{stringLengthAttr.MinimumLength},{stringLengthAttr.MaximumLength}", stringLengthAttr.ErrorMessage ?? $"{field.Name} must be between {stringLengthAttr.MinimumLength} and {stringLengthAttr.MaximumLength} characters"));
                }
                
                if (attr is RegularExpressionAttribute regexAttr)
                {
                    propSchema.Constraints.Add(new ValidationConstraint("regex", regexAttr.Pattern, regexAttr.ErrorMessage ?? $"{field.Name} must match pattern {regexAttr.Pattern}"));
                }
                
                if (attr is DescriptionAttribute descAttr)
                {
                    propSchema.Description = descAttr.Description;
                }
                
                if (attr is DefaultValueAttribute defaultValueAttr)
                {
                    propSchema.DefaultValue = defaultValueAttr.Value?.ToString();
                }
                
                if (attr is DisplayAttribute displayAttr)
                {
                    propSchema.Description = displayAttr.Description ?? propSchema.Description;
                }
            }
            
            return propSchema;
        }
        
        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(string)) return "string";
            if (type == typeof(DateTime)) return "datetime";
            if (type == typeof(Guid)) return "guid";
            
            if (type.IsArray)
            {
                return $"{GetFriendlyTypeName(type.GetElementType())}[]";
            }
            
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                var genericTypeName = type.GetGenericTypeDefinition().Name;
                
                // Remove the `1, `2, etc.
                genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
                
                var args = string.Join(", ", genericArgs.Select(GetFriendlyTypeName));
                return $"{genericTypeName}<{args}>";
            }
            
            return type.Name;
        }
        
        private bool ValidateConstraint(ValidationConstraint constraint, object value, Type valueType)
        {
            if (value == null)
            {
                // Null values fail all constraints except "required", which is handled separately
                return constraint.Type != "required";
            }
            
            switch (constraint.Type)
            {
                case "required":
                    return true; // Already checked for null
                
                case "range":
                    var rangeParts = constraint.Value.Split(',');
                    if (rangeParts.Length != 2)
                        return false;
                        
                    if (valueType == typeof(int))
                    {
                        int min = int.Parse(rangeParts[0]);
                        int max = int.Parse(rangeParts[1]);
                        int val = (int)value;
                        return val >= min && val <= max;
                    }
                    else if (valueType == typeof(float))
                    {
                        float min = float.Parse(rangeParts[0]);
                        float max = float.Parse(rangeParts[1]);
                        float val = (float)value;
                        return val >= min && val <= max;
                    }
                    else if (valueType == typeof(double))
                    {
                        double min = double.Parse(rangeParts[0]);
                        double max = double.Parse(rangeParts[1]);
                        double val = (double)value;
                        return val >= min && val <= max;
                    }
                    else if (valueType == typeof(DateTime))
                    {
                        DateTime min = DateTime.Parse(rangeParts[0]);
                        DateTime max = DateTime.Parse(rangeParts[1]);
                        DateTime val = (DateTime)value;
                        return val >= min && val <= max;
                    }
                    
                    return false;
                
                case "stringLength":
                    if (value is not string strValue)
                        return false;
                        
                    var lengthParts = constraint.Value.Split(',');
                    if (lengthParts.Length != 2)
                        return false;
                        
                    int minLength = int.Parse(lengthParts[0]);
                    int maxLength = int.Parse(lengthParts[1]);
                    
                    return strValue.Length >= minLength && strValue.Length <= maxLength;
                
                case "regex":
                    if (value is not string regexValue)
                        return false;
                        
                    var regex = new System.Text.RegularExpressions.Regex(constraint.Value);
                    return regex.IsMatch(regexValue);
                
                default:
                    _logger?.Log(LogLevel.Warning, $"Unknown constraint type: {constraint.Type}","Serialization");
                    return true; // Unknown constraints pass by default
            }
        }
    }
}