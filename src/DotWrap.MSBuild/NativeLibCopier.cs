using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using DotWrap.Configuration.Python;

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
        var assembly = Assembly.LoadFrom(dllPath);

        var allPythonGlobalConfigs = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DotWrapPythonGlobalConfig)))
            .Select(t => (DotWrapPythonGlobalConfig)Activator.CreateInstance(t)!)
            .ToList();

        if (allPythonGlobalConfigs.Count > 1)
        {
            throw new InvalidOperationException(
                $"Expected one or zero global Python configuration classes, but found {allPythonGlobalConfigs.Count}. Configuration classes found: {string.Join(", ", allPythonGlobalConfigs.Select(c => c.GetType().Name))}"
            );
        }
        var pythonGlobalConfig = allPythonGlobalConfigs.SingleOrDefault();
        PythonProjectInfo pythonProjectInfo = new(projectInfo, pythonGlobalConfig);

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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !fileName.StartsWith("lib"))
            {
                Logger.LogInfo($"Renaming {file} to lib{fileName}");
                fileName = "lib" + fileName;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !fileName.StartsWith("lib"))
            {
                Logger.LogInfo($"Creating copy of file {file} as lib{fileName}");
                var destFileLib = Path.Combine(dotWrapGeneratedRoot, "lib" + fileName);
                File.Copy(file, destFileLib, overwrite: true);
            }
            var destFile = Path.Combine(dotWrapGeneratedRoot, fileName);
            File.Copy(file, destFile, overwrite: true);
        }
    }
}
