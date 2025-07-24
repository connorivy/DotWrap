using System.IO;

namespace DotWrap.MSBuild;

/// <summary>
/// Given the path to a published dll, this class will copy the library and
/// its dependencies to the user's python package.
/// </summary>
public class NativeLibCopier(Logger logger)
{
    public void CopyNativeLibs(string dllPath)
    {
        CSharpProjectInfo projectInfo = new(dllPath);
        PythonProjectInfo pythonProjectInfo = new(projectInfo);

        var libDirectory = projectInfo.NativeLibsDirectory;
        var pythonProjectRoot = pythonProjectInfo.PythonProjectRoot;

        logger.LogInfo($"Copying native libraries from {libDirectory} to {pythonProjectRoot}");

        // copy all files in the native libs directory to the python package root
        foreach (var file in Directory.GetFiles(libDirectory))
        {
            if (Path.GetExtension(file) == ".pdb")
            {
                logger.LogInfo($"Skipping PDB file: {file}");
                continue; // skip PDB files
            }
            logger.LogInfo($"Copying {file} to {pythonProjectRoot}");

            var destFile = Path.Combine(pythonProjectRoot, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }
    }
}
