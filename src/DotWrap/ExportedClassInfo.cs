using System.Collections.Generic;

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

public class ExportedMethodInfo
{
    public required string Name { get; set; }
    public required string ReturnType { get; set; }
    public string? SummaryComment { get; set; }
    public string? ReturnsComment { get; set; }
    public List<ExportedParameterInfo> Parameters { get; set; } = new();
}

public class ExportedParameterInfo
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public string? Comment { get; set; }
}

public class ExportedPropertyInfo
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required bool HasGetter { get; set; }
    public required bool HasSetter { get; set; }
    public string? Comment { get; set; }
}
