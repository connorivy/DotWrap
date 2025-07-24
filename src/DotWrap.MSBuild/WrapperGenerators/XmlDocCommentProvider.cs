using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace DotWrap.MSBuild;

/// <summary>
/// Loads and provides access to XML documentation comments for C# types, methods, and parameters.
/// </summary>
public class XmlDocCommentProvider
{
    private readonly Dictionary<string, XElement> _memberElements;

    public XmlDocCommentProvider(string xmlDocPath)
    {
        var doc = XDocument.Load(xmlDocPath);
        _memberElements = new Dictionary<string, XElement>();
        foreach (var member in doc.Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name))
                _memberElements[name] = member;
        }
    }

    /// <summary>
    /// Gets the doc comment for a class/type.
    /// </summary>
    public string? GetTypeComment(Type type)
    {
        var key = $"T:{type.FullName}";
        return GetSummary(key);
    }

    /// <summary>
    /// Gets the doc comment for a method.
    /// </summary>
    public (string? summary, string? returns) GetMethodComments(MethodInfo method)
    {
        var key = GetMethodKey(method);
        if (_memberElements.TryGetValue(key, out var member))
        {
            var summary = member.Element("summary");
            var returns = member.Element("returns");
            return (summary?.Value.Trim(), returns?.Value.Trim());
        }
        return (null, null);
    }

    /// <summary>
    /// Gets the doc comment for a property.
    /// </summary>
    public string? GetPropertyComment(PropertyInfo property)
    {
        var key = $"P:{property.DeclaringType.FullName}.{property.Name}";
        return GetSummary(key);
    }

    /// <summary>
    /// Gets the doc comment for a parameter of a method.
    /// </summary>
    public string? GetParameterComment(MethodInfo method, string paramName)
    {
        var key = GetMethodKey(method);
        if (_memberElements.TryGetValue(key, out var member))
        {
            foreach (var p in member.Elements("param"))
            {
                if (p.Attribute("name")?.Value == paramName)
                    return p.Value.Trim();
            }
        }
        return null;
    }

    private string? GetSummary(string key)
    {
        if (_memberElements.TryGetValue(key, out var member))
        {
            var summary = member.Element("summary");
            return summary?.Value.Trim();
        }
        return null;
    }

    private static string GetMethodKey(MethodInfo method)
    {
        // Format: M:Namespace.Type.Method(Type1,Type2)
        var typeName = method.DeclaringType.FullName;
        var methodName = method.Name;
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return $"M:{typeName}.{methodName}";
        var paramTypes = string.Join(
            ",",
            Array.ConvertAll(parameters, p => GetParameterTypeName(p.ParameterType))
        );
        return $"M:{typeName}.{methodName}({paramTypes})";
    }

    private static string GetParameterTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().FullName;
            var genericArgs = string.Join(
                ",",
                Array.ConvertAll(type.GetGenericArguments(), GetParameterTypeName)
            );
            return $"{genericTypeName.Replace("+", ".")}[{genericArgs}]";
        }
        return type.FullName.Replace("+", ".");
    }
}
