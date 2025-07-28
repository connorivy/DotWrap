using System;
using System.Collections.Generic;

namespace DotWrap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapExposeAttribute(string? alias = null) : Attribute
{
    internal string? alias { get; } = alias;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapIgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapGeneratedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapMetaAttribute(string? alias = null) : Attribute
{
    public string? alias { get; } = alias;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalExposeAttribute(Type typeToWrap, string? alias = null) : Attribute
{
    public Type typeToWrap { get; } = typeToWrap;
    public string? alias { get; } = alias;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalMethodMeta(
    Type containingType,
    string methodName,
    Type[]? parameters = null,
    string? alias = null
) : DotWrapMetaAttribute(alias)
{
    public Type containingType { get; } = containingType;
    public string methodName { get; } = methodName;
    public Type[]? parameters { get; } = parameters;
}
