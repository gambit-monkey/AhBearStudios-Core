using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Attributes;

namespace AhBearStudios.Core.DependencyInjection.Models;

/// <summary>
/// Represents an injectable member with its associated attribute and type information.
/// </summary>
public sealed class InjectableMember
{
    /// <summary>
    /// Gets the member info (constructor, property, field, or method).
    /// </summary>
    public MemberInfo Member { get; }

    /// <summary>
    /// Gets the injection attribute associated with this member.
    /// </summary>
    public DIInjectAttribute Attribute { get; }

    /// <summary>
    /// Gets the type of injectable member.
    /// </summary>
    public InjectableMemberType MemberType { get; }

    /// <summary>
    /// Initializes a new injectable member.
    /// </summary>
    public InjectableMember(MemberInfo member, DIInjectAttribute attribute, InjectableMemberType memberType)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
        Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        MemberType = memberType;
    }
}