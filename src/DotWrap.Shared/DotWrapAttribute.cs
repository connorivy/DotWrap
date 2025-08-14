using System;
using System.Collections.Generic;

namespace DotWrap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapExposeAttribute(string? alias = null, string? namespaceAlias = null) : Attribute
{
    internal string? alias { get; } = alias;
    internal string? namespaceAlias { get; } = namespaceAlias;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapIgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapGeneratedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapGeneratedEnumMetaAttribute : DotWrapGeneratedAttribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapGeneratedClassWrapperAttribute : DotWrapGeneratedAttribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapMetaAttribute(string? alias = null, string? namespaceAlias = null) : Attribute
{
    public string? alias { get; } = alias;
    public string? namespaceAlias { get; } = namespaceAlias;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalExposeAttribute(
    Type typeWithMetadata,
    string? alias = null,
    string? namespaceAlias = null
) : DotWrapExposeAttribute(alias, namespaceAlias)
{
    public Type typeWithMetadata { get; } = typeWithMetadata;
}

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
