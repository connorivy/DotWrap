using System.Collections.Generic;
using System.Linq;
using static DotWrap.Internal.Constants;

namespace DotWrap.MSBuild;

public class ExportedClassInfo
{
    public required string Namespace { get; set; }
    public required string ClassName { get; set; }
    public required string EntryPrefix { get; set; }
    public string? SummaryComment { get; set; }
    public List<ExportedMethodInfo> Methods { get; set; } = new();
    public List<ExportedPropertyInfo> Properties { get; set; } = new();
}

public class ExportedMethodInfo : IHasOriginalAndExposedTypes
{
    public required string Name { get; set; }

    /// <summary>
    /// The original type of the method's return value.
    /// </summary>
    public required string OriginalType { get; set; }

    /// <summary>
    /// The return type exposed by the C API.
    /// If the original type is the same as the exposed type, this will be null.
    /// </summary>
    public required string? ExposedTypeIfDifferent { get; set; }
    public string? SummaryComment { get; set; }
    public string? ReturnsComment { get; set; }
    public List<ExportedParameterInfo> Parameters { get; set; } = new();

    public bool IsStatic => this.Parameters.FirstOrDefault()?.Name == SelfPointerName;
}

public class ExportedParameterInfo : IHasOriginalAndExposedTypes
{
    public required string Name { get; set; }
    public required string OriginalType { get; set; }
    public required string? ExposedTypeIfDifferent { get; set; }
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

public interface IHasOriginalAndExposedTypes
{
    public string OriginalType { get; }
    public string? ExposedTypeIfDifferent { get; }
}
