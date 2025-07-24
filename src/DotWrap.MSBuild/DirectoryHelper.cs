using System;
using System.IO;

namespace DotWrap.MSBuild;

public record PythonProjectInfo
{
    public PythonProjectInfo(CSharpProjectInfo cSharpProjectInfo)
    {
        CSharpProjectInfo = cSharpProjectInfo;
        ProjectName = CSharpProjectInfo.LibName.Replace(".", "_");

        var projectDir = CSharpProjectInfo.ProjectRoot;
        // get directory before the /bin dir
        Directory.CreateDirectory(Path.Combine(projectDir, "python"));
        Directory.CreateDirectory(Path.Combine(projectDir, "python", ProjectName));
    }

    public CSharpProjectInfo CSharpProjectInfo { get; init; }
    public string ProjectName { get; init; }
    public string PythonPackageRoot => Path.Combine(CSharpProjectInfo.ProjectRoot, "python");
    public string PythonProjectRoot => Path.Combine(PythonPackageRoot, ProjectName);
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
