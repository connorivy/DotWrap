using System;

namespace DotWrap;

[AttributeUsage(AttributeTargets.Class)]
public class DotWrapExposeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class DotWrapIgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class DotWrapGeneratedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class DotWrapMetaAttribute(string? alias = null) : Attribute
{
    public string? Alias { get; } = alias;
}
