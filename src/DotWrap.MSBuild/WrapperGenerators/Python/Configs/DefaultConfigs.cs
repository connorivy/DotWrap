using DotWrap.Configuration.Python;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Configs;

internal static class DefaultConfigs
{
    public static IEnumerable<DotWrapPythonTypeConfig> GetDefaultConfigs()
    {
        yield return new TaskConfig();
        yield return new ValueTaskConfig();
        yield return new ICollectionConfig();
        yield return new IReadOnlyCollectionConfig();
        yield return new NullableConfig();
    }
}
