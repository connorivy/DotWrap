namespace DotWrap.MSBuild.WrapperGenerators.Python;

internal static class PythonConstants
{
    internal const string InternalPythonPrefix = "_dotwrap_";
    internal const string Ptr = InternalPythonPrefix + "ptr";
    internal const string Lib = InternalPythonPrefix + "lib";
    internal const string Ffi = InternalPythonPrefix + "ffi";
    internal const string FromPtr = InternalPythonPrefix + "from_ptr";
    internal const string PyCreate = InternalPythonPrefix + "create";
    internal const string PyDestroy = InternalPythonPrefix + "destroy";
}
