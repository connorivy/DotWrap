using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DotWrap.MSBuild;

public class ExportedClassInfo
{
    public required string Namespace { get; set; }
    public required string ClassName { get; set; }
    public required string EntryPrefix { get; set; }
    public required bool IsStatic { get; set; }
    public required Dictionary<string, string> GenericTypeArgumentsToParameters { get; set; }
    public required List<string> Interfaces { get; set; }
    public required ClassSpecialCaseFlags SpecialCaseFlags { get; set; }
    public string? SummaryComment { get; set; }
    public List<ExportedMethodInfo> Methods { get; set; } = new();
    public List<ExportedPropertyInfo> Properties { get; set; } = new();

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

public class ExportedMethodInfo : IHasOriginalAndExposedTypes
{
    public required string OriginalName { get; set; }

    private string? stampedName;
    public string StampedName => stampedName ??= this.OriginalName + this.GetParameterStamp();

    /// <summary>
    /// The original type of the method's return value.
    /// </summary>
    public required string OriginalType { get; set; }

    /// <summary>
    /// The return type exposed by the C API.
    /// If the original type is the same as the exposed type, this will be null.
    /// </summary>
    public required string? ExposedTypeIfDifferent { get; set; }
    public required string? GenericTypeName { get; set; }
    public required bool IsStatic { get; set; }
    public string? SummaryComment { get; set; }
    public string? ReturnsComment { get; set; }
    public required MethodSpecialCaseFlags SpecialCaseFlags { get; set; }
    public required List<ExportedParameterInfo> Parameters { get; set; } = new();

    /// <summary>
    /// Generates a unique stamp for the method based on the original type of it's parameters.
    /// </summary>
    /// <returns></returns>
    public string GetParameterStamp()
    {
        if (Parameters.Count == 0)
        {
            return string.Empty;
        }

        string Key = string.Join("", Parameters.Select(p => p.OriginalType));
        unchecked
        {
            const int seed = 0x811C9DC;
            const int prime = 16777619;
            int hash = seed;

            foreach (char c in Key)
                hash = (hash ^ c) * prime;

            return $"_{Math.Abs(hash):X8}";
        }
    }
}

public class ExportedParameterInfo : IHasOriginalAndExposedTypes
{
    public required string Name { get; set; }
    public required string OriginalType { get; set; }
    public required string? ExposedTypeIfDifferent { get; set; }
    public required string? GenericTypeName { get; set; }
    public string? Comment { get; set; }
}

public class ExportedPropertyInfo : IHasOriginalAndExposedTypes
{
    public required string Name { get; set; }
    public required string OriginalType { get; set; }
    public required string? ExposedTypeIfDifferent { get; set; }
    public required bool HasGetter { get; set; }
    public required bool HasSetter { get; set; }
    public string? Comment { get; set; }
}

[Flags]
public enum MethodSpecialCaseFlags
{
    None = 0,
    PropertyGetter = 1 << 0,
    PropertySetter = 1 << 1,
    Static = 1 << 2,
    Indexer = 1 << 3,
}

public interface IHasOriginalAndExposedTypes
{
    public string OriginalType { get; }
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
