using System.IO;

namespace DotWrap.MSBuild;

/// <summary>
/// Given the path to a published dll, this class will copy the library and
/// its dependencies to the user's python package.
/// </summary>
public class NativeLibCopier
{
    public void CopyNativeLibs(string dllPath)
    {
        CSharpProjectInfo projectInfo = new(dllPath);
        PythonProjectInfo pythonProjectInfo = new(projectInfo);

        var libDirectory = projectInfo.NativeLibsDirectory;
        var dotWrapGeneratedRoot = pythonProjectInfo.DotWrapGeneratedRoot;

        Logger.LogInfo($"Copying native libraries from {libDirectory} to {dotWrapGeneratedRoot}");

        // copy all files in the native libs directory to the python package root
        foreach (var file in Directory.GetFiles(libDirectory))
        {
            if (Path.GetExtension(file) == ".pdb")
            {
                Logger.LogInfo($"Skipping PDB file: {file}");
                continue; // skip PDB files
            }
            Logger.LogInfo($"Copying {file} to {dotWrapGeneratedRoot}");

            var destFile = Path.Combine(dotWrapGeneratedRoot, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }
    }
}
