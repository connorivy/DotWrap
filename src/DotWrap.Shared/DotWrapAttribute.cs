using System;
using System.Collections.Generic;

namespace DotWrap;

/// <summary>
/// Attribute to mark a type for exposure in the generated package.
/// </summary>
/// <param name="alias">optional alias for the generated type name</param>
/// <param name="namespaceAlias">optional alias for the namespace of the type within the generated package</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class DotWrapExposeAttribute(string? alias = null, string? namespaceAlias = null) : Attribute
{
    internal string? alias { get; } = alias;
    internal string? namespaceAlias { get; } = namespaceAlias;
}

/// <summary>
/// Attribute to mark a type for exposure in the generated package.
/// </summary>
/// <param name="alias">optional alias for the generated type name</param>
/// <param name="namespaceAlias">optional alias for the namespace of the type within the generated package</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExposeAssemblyAttribute(Type? assemblyType = null) : Attribute
{
    public Type? assemblyType { get; } = assemblyType;
}

/// <summary>
/// Attribute to mark a method for exclusion from the generated package even though the type will be included
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapIgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapMetaAttribute(string? alias = null, string? namespaceAlias = null) : Attribute
{
    public string? alias { get; } = alias;
    public string? namespaceAlias { get; } = namespaceAlias;
}

/// <summary>
/// Attribute that will modify the generation of an external type.
/// </summary>
/// <param name="typeWithMetadata"></param>
/// <param name="alias"></param>
/// <param name="namespaceAlias"></param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalTypeConfigAttribute(
    Type typeWithMetadata,
    string? alias = null,
    string? namespaceAlias = null
) : DotWrapExposeAttribute(alias, namespaceAlias)
{
    public Type typeWithMetadata { get; } = typeWithMetadata;
}

/// <summary>
/// Attribute to mark an external method for exposure in the generated package.
/// i.e. [assembly: DotWrapExternalMethodMeta(typeof(List<>), nameof(List<int>.Add))] will include the 'Add' method
/// </summary>
/// <param name="containingType"></param>
/// <param name="methodName"></param>
/// <param name="parameters"></param>
/// <param name="alias"></param>
/// <param name="ignore"></param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalMethodMeta(
    Type containingType,
    string methodName,
    Type[]? parameters = null,
    string? alias = null,
    bool ignore = false
) : DotWrapMetaAttribute(alias)
{
    public Type containingType { get; } = containingType;
    public string methodName { get; } = methodName;
    public Type[]? parameters { get; } = parameters;
    public bool ignore { get; } = ignore;
}

/// <summary>
/// Attribute to mark an external property for exposure in the generated package.
/// i.e. [assembly: DotWrapExternalPropertyMeta(typeof(List<>), nameof(List<int>.Count))] will include the 'Count' property
/// </summary>
/// <param name="containingType"></param>
/// <param name="propertyName"></param>
/// <param name="propertyType"></param>
/// <param name="alias"></param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalPropertyMeta(
    Type containingType,
    string propertyName,
    PropertyType propertyType = PropertyType.GetAndSet,
    string? alias = null
) : DotWrapMetaAttribute(alias)
{
    public Type containingType { get; } = containingType;
    public string propertyName { get; } = propertyName;
    public PropertyType propertyType { get; } = propertyType;
}

/// <summary>
/// Attribute to mark an external property for exposure in the generated package.
/// i.e. [assembly: DotWrapExternalIndexerMeta(typeof(List<>))] will include the list[x] operation in the generated package.
/// </summary>
/// <param name="containingType"></param>
/// <param name="propertyType"></param>
/// <param name="alias"></param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalIndexerMeta(
    Type containingType,
    PropertyType propertyType = PropertyType.GetAndSet,
    string? alias = null
) : DotWrapMetaAttribute(alias)
{
    public Type containingType { get; } = containingType;

    public PropertyType propertyType { get; } = propertyType;
}

[Flags]
public enum PropertyType
{
    None = 0,
    Get = 1 << 0,
    Set = 1 << 1,
    GetAndSet = Get | Set,
}
