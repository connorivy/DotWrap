using DotWrap.Utils;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class ModuleBuilder(PythonProjectInfo pythonProjectInfo)
{
    private static readonly Dictionary<string, string> offLimitsModuleNames = new()
    {
        { "global", "globals" },
    };

    public Dictionary<string, InitFileBuilder> Modules { get; } = new();
    public InitFileBuilder RootImportFile { get; } = new(new(), new(), true);

    public InitFileBuilder GetImportFile(string csNamespace)
    {
        var namespaceParts = pythonProjectInfo.MapNamespacePartToModule(csNamespace);
        var previousImportFile = RootImportFile;

        foreach (var part in namespaceParts)
        {
            var mappedPart = offLimitsModuleNames.GetValueOrDefault(part, part);
            var currentImportFile = GetOrCreateImportFile(mappedPart);
            previousImportFile.AddModuleImport(mappedPart);
            previousImportFile = currentImportFile;
        }

        return previousImportFile;
    }

    private InitFileBuilder GetOrCreateImportFile(string moduleName)
    {
        if (!Modules.TryGetValue(moduleName, out var moduleBuilder))
        {
            moduleBuilder = new InitFileBuilder(new(), new(), false);
            Modules[moduleName] = moduleBuilder;
        }
        return moduleBuilder;
    }
}

public class InitFileBuilder(
    IndentedPythonStringBuilder builder,
    HashSet<string> importedNames,
    bool isRoot
)
{
    public void AddModuleImport(string moduleName)
    {
        if (!importedNames.Add(moduleName))
        {
            return;
        }

        builder.AppendLine($"from .{(isRoot ? "modules" : "")} import {moduleName}");
    }

    public void AddTypeImport(string typeName)
    {
        builder.AppendLine($"from .{(isRoot ? "" : ".")}main import {typeName}");
    }

    public override string ToString() => builder.ToString();
}
