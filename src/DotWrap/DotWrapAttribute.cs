using System;

namespace DotWrap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapExposeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapIgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapGeneratedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapMetaAttribute(string? alias = null) : Attribute
{
    public string? alias { get; } = alias;
}
