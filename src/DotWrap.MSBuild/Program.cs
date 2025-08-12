using System;
using System.IO;
using DotWrap.MSBuild;
using DotWrap.MSBuild.WrapperGenerators;

#if DEBUG
// System.Diagnostics.Debugger.Launch();
#endif

string dllPath = args[0];
var operationMagicString = args[1];

string dllDirectory =
    Path.GetDirectoryName(dllPath) ?? throw new ArgumentException("Invalid DLL path.");

string logPath = Path.Combine(dllDirectory, $"DotWrapOutput{operationMagicString}.log");

try
{
    switch (operationMagicString)
    {
        case MagicStrings.BuildOperation:
            var wrapperGenerator = new WrapperGeneratorFromEmbeddedMetadata();
            wrapperGenerator.GenerateWrapper(dllPath);
            break;
        case MagicStrings.PublishOperation:
            var nativeLibCopier = new NativeLibCopier();
            nativeLibCopier.CopyNativeLibs(dllPath);
            break;
        default:
            throw new ArgumentException(
                $"Operation {args[1]} does not match any known operations."
            );
    }
}
catch (Exception ex)
{
    Logger.LogError(ex.ToString());
    throw;
}
finally
{
    Logger.SaveToFile(logPath);
}
