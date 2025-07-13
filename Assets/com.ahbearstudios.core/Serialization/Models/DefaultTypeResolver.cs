using System.Collections.Generic;
using System.Linq;

namespace AhBearStudios.Core.Serialization.Models;

/// <summary>
/// Default type resolver implementation.
/// </summary>
public class DefaultTypeResolver : ITypeResolver
{
    private readonly Dictionary<string, Type> _typeCache = new();
    private readonly Dictionary<Type, string> _nameCache = new();

    /// <inheritdoc />
    public Type ResolveType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

        if (_typeCache.TryGetValue(typeName, out var cachedType))
            return cachedType;

        var type = Type.GetType(typeName) ?? 
                   AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(a => a.GetTypes())
                       .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);

        if (type != null)
        {
            _typeCache[typeName] = type;
        }

        return type;
    }

    /// <inheritdoc />
    public string GetTypeName(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (_nameCache.TryGetValue(type, out var cachedName))
            return cachedName;

        var name = type.FullName ?? type.Name;
        _nameCache[type] = name;
        return name;
    }
}