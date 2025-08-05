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
        var exportedClasses = new List<ExportedClassInfo>();
        List<ExportedEnumInfo> exportedEnums = [];

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

            if (attr is DotWrapGeneratedEnumMetaAttribute enumAttr)
            {
                AddEnumWrapperInfo(exportedEnums, type);
            }
            else if (attr is DotWrapGeneratedClassWrapperAttribute classAttr)
            {
                AddClassWrapperInfo(exportedClasses, type);
            }
        }

        CffiApiWrapperBuilder pythonWrapperBuilder = new(projectInfo);
        pythonWrapperBuilder.BuildWrapper(exportedClasses, exportedEnums);
    }

    private static void AddClassWrapperInfo(List<ExportedClassInfo> exportedClasses, Type type)
    {
        string classInfoString = (string)(
            type.GetField(ClassMetadata, BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null)
            ?? throw new InvalidOperationException(
                $"Type {type.FullName} does not have a static field '{ClassMetadata}'."
            )
        );
        ExportedClassInfo classInfo =
            JsonSerializer.Deserialize<ExportedClassInfo>(
                classInfoString,
                DotWrapSerializerOptions.Default
            )
            ?? throw new InvalidOperationException(
                $"Failed to deserialize class info for {type.FullName}."
            );

        exportedClasses.Add(classInfo);
    }

    private static void AddEnumWrapperInfo(List<ExportedEnumInfo> exportedEnums, Type type)
    {
        string classInfoString = (string)(
            type.GetField(ClassMetadata, BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null)
            ?? throw new InvalidOperationException(
                $"Type {type.FullName} does not have a static field '{ClassMetadata}'."
            )
        );
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
}
