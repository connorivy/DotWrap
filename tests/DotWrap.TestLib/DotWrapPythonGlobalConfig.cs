namespace DotWrap.TestLib;

internal class GlobalConfig : DotWrap.Configuration.Python.DotWrapPythonGlobalConfig
{
    public override string? PythonPackageName => "testlib";

    public override Dictionary<string, string>? NamespaceOverrides { get; } =
        new() { { "System.Threading.Tasks", "builtin.threading" } };
}
