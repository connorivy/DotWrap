using System.Reflection;
using System.Text.Json;
using DotWrap.Configuration;
using DotWrap.MSBuild.WrapperGenerators.Python.Builders;
using static DotWrap.Internal.Constants;

namespace DotWrap.MSBuild.WrapperGenerators;

public class WrapperGeneratorFromEmbeddedMetadata
{
    public void GenerateWrapper(string libFullPath)
    {
        Logger.LogInfo($"Loading assembly from {libFullPath}");
        var assembly = Assembly.LoadFrom(libFullPath);

        CSharpProjectInfo projectInfo = new(libFullPath);
        Dictionary<string, ExportedTypeDefinition> exportedTypes = [];

        Logger.LogDebug($"Processing assembly with {assembly.GetTypes().Length} types");
        foreach (var type in assembly.GetTypes())
        {
            Logger.LogDebug($"Type {type.FullName}");
        }

        // reflection strangely represents static classes as abstract sealed classes
        foreach (
            var type in assembly.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed)
        )
        {
            Logger.LogInfo($"assessing type {type.FullName}");
            var attr = type.GetCustomAttribute<DotWrapGeneratedAttribute>();
            if (attr == null)
            {
                continue;
            }
            Logger.LogInfo($"Processing type {type.FullName} with attribute {attr.GetType().Name}");

            var classInfoString = (string)(
                type.GetField(Metadata, BindingFlags.NonPublic | BindingFlags.Static)
                    ?.GetValue(null)
                ?? throw new InvalidOperationException(
                    $"Type {type.FullName} does not have a static field '{Metadata}'."
                )
            );
            if (attr is DotWrapGeneratedEnumMetaAttribute enumAttr)
            {
                AddEnumWrapperInfo(exportedTypes, type, classInfoString);
            }
            // else if (attr is DotWrapGeneratedClassWrapperAttribute classAttr)
            // {
            //     AddClassWrapperInfo(exportedClasses, type, classInfoString);
            // }
            else if (attr is DotWrapGeneratedClassWrapperAttribute)
            {
                AddExportedTypeInfo(exportedTypes, type, classInfoString);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName} has an unsupported DotWrap attribute: {attr.GetType().Name}."
                );
            }
        }

        var configTypes = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DotWrapPythonTypeConfig)))
            .Select(t => (DotWrapPythonTypeConfig)Activator.CreateInstance(t)!)
            .ToDictionary(t => t.TypeToConfigure);

        GlobalContext globalContext = new(
            exportedTypes,
            // [.. exportedEnums.Select(e => $"{e.Namespace}.{e.Name}")],
            configTypes,
            new List<OutParamInfo>()
        );
        CffiApiWrapperBuilder pythonWrapperBuilder = new(globalContext, projectInfo);
        pythonWrapperBuilder.BuildWrapper(
            exportedTypes.Values.Where(t => t is not ExportedEnumInfo).ToList(),
            exportedTypes.Values.OfType<ExportedEnumInfo>().ToList()
        );
    }

    private static void AddClassWrapperInfo(
        List<ExportedTypeDefinition> exportedClasses,
        Type type,
        string classInfoString
    )
    {
        ExportedTypeDefinition classInfo =
            JsonSerializer.Deserialize<ExportedTypeDefinition>(
                classInfoString,
                DotWrapSerializerOptions.Default
            )
            ?? throw new InvalidOperationException(
                $"Failed to deserialize class info for {type.FullName}."
            );

        exportedClasses.Add(classInfo);
    }

    private static void AddEnumWrapperInfo(
        Dictionary<string, ExportedTypeDefinition> exportedTypes,
        // List<ExportedEnumInfo> exportedEnums,
        Type type,
        string classInfoString
    )
    {
        ExportedEnumInfo enumInfo =
            JsonSerializer.Deserialize<ExportedEnumInfo>(
                classInfoString,
                DotWrapSerializerOptions.Default
            )
            ?? throw new InvalidOperationException(
                $"Failed to deserialize class info for {type.FullName}."
            );

        // exportedEnums.Add(enumInfo);
        exportedTypes[enumInfo.Id.ToString()] = enumInfo;
    }

    private static void AddExportedTypeInfo(
        Dictionary<string, ExportedTypeDefinition> exportedTypes,
        Type type,
        string classInfoString
    )
    {
        var typeInfo =
            JsonSerializer.Deserialize<ExportedTypeDefinition>(
                classInfoString,
                DotWrapSerializerOptions.Default
            )
            ?? throw new InvalidOperationException(
                $"Failed to deserialize class info for {type.FullName}."
            );

        exportedTypes[typeInfo.Id.ToString()] = typeInfo;
    }
}
