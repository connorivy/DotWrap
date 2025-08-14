using System;
using System.IO;
using DotWrap.Configuration.Python;

namespace DotWrap.MSBuild;

public record PythonProjectInfo
{
    private const string ProjectRootDir = "python_project_root";
    public const string DotWrapGeneratedDir = "__dotwrap_generated";
    public const string DotWrapExports = "__dotwrap_exports";
    private string[] projectRootParts;
    private readonly DotWrapPythonGlobalConfig? pythonGlobalConfig;

    public PythonProjectInfo(
        CSharpProjectInfo cSharpProjectInfo,
        DotWrapPythonGlobalConfig? pythonGlobalConfig
    )
    {
        CSharpProjectInfo = cSharpProjectInfo;
        this.pythonGlobalConfig = pythonGlobalConfig;
        projectRootParts = CSharpProjectInfo.LibName.Split('.');
        ProjectName =
            pythonGlobalConfig?.PythonPackageName
            ?? CSharpProjectInfo.LibName.Replace(".", "_").ToLowerInvariant();

        Directory.CreateDirectory(PythonProjectRoot);
        Directory.CreateDirectory(PythonPackageRoot);
        Directory.CreateDirectory(DotWrapGeneratedRoot);
    }

    public CSharpProjectInfo CSharpProjectInfo { get; init; }
    public string ProjectName { get; init; }
    public string PythonProjectRoot => Path.Combine(CSharpProjectInfo.ProjectRoot, ProjectRootDir);
    public string PythonPackageRoot => Path.Combine(PythonProjectRoot, ProjectName);
    public string DotWrapGeneratedRoot => Path.Combine(PythonPackageRoot, DotWrapGeneratedDir);

    public IEnumerable<string> MapNamespacePartToModule(string csNamespace)
    {
        foreach (var namespaceReplace in pythonGlobalConfig?.NamespaceOverrides ?? [])
        {
            csNamespace = csNamespace.Replace(namespaceReplace.Key, namespaceReplace.Value);
        }

        var sameAsProjectName = true;
        var loopCount = -1;
        foreach (var part in csNamespace.Split('.'))
        {
            loopCount++;
            if (
                sameAsProjectName
                && loopCount < projectRootParts.Length
                && part.Equals(projectRootParts[loopCount], StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }
            sameAsProjectName = false;
            yield return part.ToLowerInvariant(); // todo: less opinionated
        }
    }
}

public record CSharpProjectInfo
{
    public CSharpProjectInfo(string libTargetPath)
    {
        TargetPath = libTargetPath;
        LibName = Path.GetFileNameWithoutExtension(libTargetPath);
        ProjectRoot = ProjectRoot = libTargetPath.Split(["bin"], StringSplitOptions.None)[0];
    }

    public string TargetPath { get; init; }
    public string LibName { get; init; }
    public string ProjectRoot { get; init; }
    public string TargetLibDirectory =>
        Path.GetDirectoryName(TargetPath) ?? throw new ArgumentException("Invalid library path.");
    public string XmlDocPath => Path.Combine(TargetLibDirectory, LibName + ".xml");
    public string NativeLibsDirectory =>
        Path.Combine(
            TargetLibDirectory ?? throw new ArgumentException("Invalid library path."),
            "native"
        );
}
