using System;
using System.IO;

namespace DotWrap.MSBuild;

public record PythonProjectInfo
{
    private const string ProjectRootDir = "python_project_root";
    public const string DotWrapGeneratedDir = "dotwrap_generated";

    public PythonProjectInfo(CSharpProjectInfo cSharpProjectInfo)
    {
        CSharpProjectInfo = cSharpProjectInfo;
        ProjectName = CSharpProjectInfo.LibName.Replace(".", "_").ToLowerInvariant();

        Directory.CreateDirectory(PythonProjectRoot);
        Directory.CreateDirectory(PythonPackageRoot);
        Directory.CreateDirectory(DotWrapGeneratedRoot);
    }

    public CSharpProjectInfo CSharpProjectInfo { get; init; }
    public string ProjectName { get; init; }
    public string PythonProjectRoot => Path.Combine(CSharpProjectInfo.ProjectRoot, ProjectRootDir);
    public string PythonPackageRoot => Path.Combine(PythonProjectRoot, ProjectName);
    public string DotWrapGeneratedRoot => Path.Combine(PythonPackageRoot, DotWrapGeneratedDir);
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
