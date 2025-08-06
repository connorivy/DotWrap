using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DotWrap.MSBuild.WrapperGenerators.Python.Builders;
using static DotWrap.Internal.Constants;

namespace DotWrap.MSBuild.WrapperGenerators;

public class WrapperGeneratorFromEmbeddedMetadata(Logger logger)
{
    public void GenerateWrapper(string libFullPath)
    {
        logger.LogInfo($"Loading assembly from {libFullPath}");
        var assembly = Assembly.LoadFrom(libFullPath);

        CSharpProjectInfo projectInfo = new(libFullPath);
        List<ExportedEnumInfo> exportedEnums = [];
        Dictionary<string, ExportedTypeDefinitionInfo> exportedTypes = [];

        // reflection strangely represents static classes as abstract sealed classes
        foreach (
            var type in assembly.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed)
        )
        {
            var attr = type.GetCustomAttribute<DotWrapGeneratedAttribute>();
            if (attr == null)
            {
                continue;
            }

            var classInfoString = (string)(
                type.GetField(Metadata, BindingFlags.NonPublic | BindingFlags.Static)
                    ?.GetValue(null)
                ?? throw new InvalidOperationException(
                    $"Type {type.FullName} does not have a static field '{Metadata}'."
                )
            );
            if (attr is DotWrapGeneratedEnumMetaAttribute enumAttr)
            {
                AddEnumWrapperInfo(exportedEnums, type, classInfoString);
            }
            // else if (attr is DotWrapGeneratedClassWrapperAttribute classAttr)
            // {
            //     AddClassWrapperInfo(exportedClasses, type, classInfoString);
            // }
            else if (attr is DotWrapGeneratedClassWrapperAttribute)
            {
                AddExportedTypeInfo(exportedTypes, type, classInfoString);
            }
        }

        GlobalContext globalContext = new(
            exportedTypes,
            [.. exportedEnums.Select(e => $"{e.Namespace}.{e.Name}")]
        );
        CffiApiWrapperBuilder pythonWrapperBuilder = new(globalContext, projectInfo);
        pythonWrapperBuilder.BuildWrapper(exportedTypes.Values.ToList(), exportedEnums);
    }

    private static void AddClassWrapperInfo(
        List<ExportedTypeDefinitionInfo> exportedClasses,
        Type type,
        string classInfoString
    )
    {
        ExportedTypeDefinitionInfo classInfo =
            JsonSerializer.Deserialize<ExportedTypeDefinitionInfo>(
                classInfoString,
                DotWrapSerializerOptions.Default
            )
            ?? throw new InvalidOperationException(
                $"Failed to deserialize class info for {type.FullName}."
            );

        exportedClasses.Add(classInfo);
    }

    private static void AddEnumWrapperInfo(
        List<ExportedEnumInfo> exportedEnums,
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

        exportedEnums.Add(enumInfo);
    }

    private static void AddExportedTypeInfo(
        Dictionary<string, ExportedTypeDefinitionInfo> exportedTypes,
        Type type,
        string classInfoString
    )
    {
        var typeInfo =
            JsonSerializer.Deserialize<ExportedTypeDefinitionInfo>(
                classInfoString,
                DotWrapSerializerOptions.Default
            )
            ?? throw new InvalidOperationException(
                $"Failed to deserialize class info for {type.FullName}."
            );

        exportedTypes[typeInfo.Id.ToString()] = typeInfo;
    }
}
