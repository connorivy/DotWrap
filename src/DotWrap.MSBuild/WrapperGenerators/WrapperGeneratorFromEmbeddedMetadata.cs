using System;
using System.Collections.Generic;
using System.Linq;
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

        var configTypes = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DotWrapPythonTypeConfig)))
            .Select(t => (DotWrapPythonTypeConfig)Activator.CreateInstance(t)!)
            .ToDictionary(t => t.TypeToConfigure);

        GlobalContext globalContext = new(
            exportedTypes,
            [.. exportedEnums.Select(e => $"{e.Namespace}.{e.Name}")],
            configTypes
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
