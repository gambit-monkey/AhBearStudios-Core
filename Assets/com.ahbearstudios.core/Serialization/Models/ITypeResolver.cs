namespace AhBearStudios.Core.Serialization.Models;

// <summary>
/// Interface for custom type resolution during serialization.
/// </summary>
public interface ITypeResolver
{
    /// <summary>
    /// Resolves a type from its string representation.
    /// </summary>
    /// <param name="typeName">Type name</param>
    /// <returns>Resolved type</returns>
    Type ResolveType(string typeName);

    /// <summary>
    /// Gets the string representation of a type.
    /// </summary>
    /// <param name="type">Type to get name for</param>
    /// <returns>Type name</returns>
    string GetTypeName(Type type);
}