using System.IO;
using System.Runtime.InteropServices;

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
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension == ".pdb" || extension == ".dbg")
            {
                Logger.LogInfo($"Skipping file: {file}");
                continue;
            }
            Logger.LogInfo($"Copying {file} to {dotWrapGeneratedRoot}");

            var fileName = Path.GetFileName(file);
            if (
                (
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ) && !fileName.StartsWith("lib")
            )
            {
                Logger.LogInfo($"Renaming {file} to lib{fileName}");
                fileName = "lib" + fileName;
            }
            var destFile = Path.Combine(dotWrapGeneratedRoot, fileName);
            File.Copy(file, destFile, overwrite: true);
        }
    }
}
