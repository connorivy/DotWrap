using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using DotWrap.Utils;

namespace DotWrap.Configuration;

public class ExportedMethodInfo : IHasOriginalAndExposedTypes
{
    public required string OriginalName { get; set; }

    public string StampedName =>
        field ??= this.OriginalName + DotWrapUtils.GetStamp(this.ParameterStringToStamp());

    /// <summary>
    /// The original type of the method's return value.
    /// </summary>
    public required string OriginalTypeName { get; set; }

    /// <summary>
    /// The return type exposed by the C API.
    /// If the original type is the same as the exposed type, this will be null.
    /// </summary>
    public required string? ExposedTypeIfDifferent { get; set; }
    public required string? GenericTypeName { get; set; }
    public required ExportedTypeInstanceInfo ReturnType { get; set; }
    public required bool IsStatic { get; set; }
    public string? SummaryComment { get; set; }
    public string? ReturnsComment { get; set; }
    public required MethodSpecialCaseFlags SpecialCaseFlags { get; set; }
    public required List<ExportedParameterInfo> Parameters { get; set; } = new();

    private string ParameterStringToStamp() =>
        string.Join(", ", Parameters.Select(p => p.OriginalTypeName));
}

public class ExportedParameterInfo : IHasOriginalAndExposedTypes
{
    public required string Name { get; set; }
    public required ExportedTypeInstanceInfo Type { get; set; }
    public required string OriginalTypeName { get; set; }
    public required string? ExposedTypeIfDifferent { get; set; }
    public required string? GenericTypeName { get; set; }
    public string? Comment { get; set; }
}

public class ExportedEnumInfo : IHasOriginalAndExposedTypes
{
    public required string Namespace { get; set; }
    public required string Name { get; set; }
    public required string OriginalTypeName { get; set; }
    public required string? ExposedTypeIfDifferent { get; set; }
    public required Dictionary<string, long> Options { get; set; }
}

public class ExportedTypeDefinitionInfo
{
    public ExportedTypeId Id => new(Namespace, TypeName, GenericTypeArgumentsToParameters.Keys);
    public required string FullyQualifiedName { get; set; }

    // public string EntryPrefix => field ??= $"c{DotWrapUtils.GetStamp(this.Id.ToString())}_";
    public required string EntryPrefix { get; set; }

    // public required string[]? GenericParameters { get; set; }
    public required Dictionary<string, string> GenericTypeArgumentsToParameters { get; set; }
    public required List<string> Interfaces { get; set; }
    public required string Namespace { get; set; }
    public required string TypeName { get; set; }
    public required ExportedType ExportedType { get; set; }
    public required TypeSpecialCaseFlags SpecialCaseFlags { get; set; }
    public string? SummaryComment { get; set; }
    public required bool IsSameAsExposedType { get; set; }
    public List<ExportedMethodInfo> Methods { get; set; } = new();

    public bool TryGetICollectionType([NotNullWhen(true)] out string? collectionType) =>
        this.TryGetSingleGenericInterfaceType(
            "System.Collections.Generic.ICollection<",
            out collectionType
        );

    public bool TryGetIReadonlyCollectionType([NotNullWhen(true)] out string? collectionType) =>
        this.TryGetSingleGenericInterfaceType(
            "System.Collections.Generic.IReadOnlyCollection<",
            out collectionType
        );

    public bool TryGetSingleGenericInterfaceType(
        string interfaceStart,
        [NotNullWhen(true)] out string? collectionType
    )
    {
        var collectionInter = this.Interfaces.FirstOrDefault(i => i.StartsWith(interfaceStart));
        if (collectionInter is not null)
        {
            collectionType = collectionInter[interfaceStart.Length..^1];
            return true;
        }
        collectionType = null;
        return false;
    }
}

public class ExportedTypeInstanceInfo
{
    public required ExportedTypeId DefinitionId { get; set; }
    public required string[]? DefinitionGenericArgs { get; set; }
    public required string? GenericName { get; set; }
    public bool IsNullable { get; set; }
}

public record ExportedTypeId
{
    public string Id { get; }

    [Obsolete("Use the constructor with namespace, typeName, and typeParams instead.")]
    [JsonConstructor]
    public ExportedTypeId(string id)
    {
        Id = id;
    }

    public ExportedTypeId(string @namespace, string typeName, params IEnumerable<string> typeArgs)
    {
        var genericPart = string.Join("_", typeArgs);
        if (!string.IsNullOrEmpty(genericPart))
        {
            genericPart = $"_{genericPart}";
        }
        Id = $"{@namespace}.{typeName}{genericPart}";
    }

    public override string ToString() => Id;
}

public enum ExportedType
{
    Undefined = 0,
    SByte,
    Byte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float,
    Double,
    IntPtr,
    Void,
    Char,
}

[Flags]
public enum MethodSpecialCaseFlags
{
    None = 0,
    PropertyGetter = 1 << 0,
    PropertySetter = 1 << 1,
    Static = 1 << 2,
    Indexer = 1 << 3,
    EnumReturnType = 1 << 4,
}

[Flags]
public enum TypeSpecialCaseFlags
{
    None = 0,
    Class = 1 << 0,
    Interface = 1 << 1,
    Struct = 1 << 2,
    Enum = 1 << 3,
    Static = 1 << 4,
}

public interface IHasOriginalAndExposedTypes
{
    public string OriginalTypeName { get; }
    public string? ExposedTypeIfDifferent { get; }
}

[Flags]
public enum ClassSpecialCaseFlags
{
    None = 0,

    IEnumerable = 1 << 0,

    /// <summary>
    /// This class implements ICollection<T>
    /// </summary>
    ICollection = 1 << 1,

    /// <summary>
    /// This class implements IList<T>
    /// </summary>
    IList = 1 << 2,

    /// <summary>
    /// </summary>
    IsReadOnly = 1 << 3,
}
