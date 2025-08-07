namespace DotWrap.MSBuild.WrapperGenerators.Python;

public static class PythonConstants
{
    public const string InternalPythonPrefix = "_dotwrap_";
    public const string Ptr = InternalPythonPrefix + "ptr";
    public const string Lib = InternalPythonPrefix + "lib";
    public const string Ffi = InternalPythonPrefix + "ffi";
    public const string FromPtr = InternalPythonPrefix + "from_ptr";
    public const string Typed = InternalPythonPrefix + "typed";
    public const string InternalPyResult = InternalPythonPrefix + "internal_result";
    public const string ExportedPyResult = InternalPythonPrefix + "exported_result";
}
