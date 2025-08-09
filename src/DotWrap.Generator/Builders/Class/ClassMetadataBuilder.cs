using DotWrap.Configuration;
using DotWrap.Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders.Class;

public class ClassMetadataBuilder
{
    // public ExportedClassInfo ClassInfo { get; }
    public ExportedTypeDefinition TypeInfo { get; }

    public ClassMetadataBuilder(ClassBuilderContext classContext)
    {
        // Dictionary<string, string> genericTypeParametersToArguments = new();
        // for (int i = 0; i < classContext.ClassSymbol.TypeParameters.Length; i++)
        // {
        //     var typeParam = classContext.ClassSymbol.TypeParameters[i];
        //     var typeArg = classContext.ClassSymbol.TypeArguments[i];
        //     genericTypeParametersToArguments[typeParam.Name] = typeArg.ToDisplayString();
        // }
        var genericTypeArgumentsToParameters = classContext.TypeParametersToArguments.ToDictionary(
            kvp => kvp.Value.ToDisplayString(),
            kvp => kvp.Key.Name
        );
        // ClassInfo = new ExportedClassInfo
        // {
        //     Namespace = classContext.Namespace,
        //     ClassName = classContext.ClassName,
        //     IsStatic = classContext.IsStatic,
        //     EntryPrefix = classContext.EntryPrefix,
        //     GenericTypeArgumentsToParameters = genericTypeArgumentsToParameters,
        //     Interfaces = classContext
        //         .ClassSymbol.AllInterfaces.Select(i => i.ToDisplayString())
        //         .Append(
        //             classContext.ClassSymbol.TypeKind == TypeKind.Interface
        //                 ? classContext.ClassSymbol.ToDisplayString()
        //                 : null
        //         )
        //         .OfType<string>()
        //         .ToList(),
        //     SpecialCaseFlags = classContext.SpecialCaseFlags,
        //     SummaryComment = XmlParser.ParseSummary(
        //         classContext.ClassSymbol.GetDocumentationCommentXml()
        //     ),
        // };
        var exportedType = classContext.ClassSymbol.GetExportedType(out var isOriginalType);
        var assemblyQualifiedName =
            DotWrap.Generator.Utils.AssemblyNameUtils.GetAssemblyQualifiedName(
                classContext.ClassSymbol
            );
        var typeDefinition = new ExportedTypeDefinition()
        {
            Id = classContext.ClassSymbol.GetExportedTypeId(),
            AssemblyQualifiedName = assemblyQualifiedName,
            SimplifiedAssemblyQualifiedName =
                DotWrap.Utils.AssemblyNameUtils.GetSimplifiedAssemblyName(assemblyQualifiedName),
            FullyQualifiedName = classContext.ClassSymbol.ToDisplayString(),
            TypeNameNoGenerics = classContext.ClassNameWithoutGenerics,
            EntryPrefix = classContext.EntryPrefix,
            ExportedType = classContext.ClassSymbol.GetExportedType(out _),
            GenericTypeArgumentsToParameters = genericTypeArgumentsToParameters,
            IsSameAsExposedType = isOriginalType,
            OriginalTypeWrapperName = classContext.WrapperName,
            // GenericParameters = classContext.TypeParameters.Select(tp => tp.Name).ToArray(),
            SpecialCaseFlags = classContext.ClassSymbol.GetSpecialCaseFlags(),
            SummaryComment = XmlParser.ParseSummary(
                classContext.ClassSymbol.GetDocumentationCommentXml()
            ),
        };

        if (
            classContext.ClassSymbol.TypeKind == TypeKind.Enum
            && classContext.ClassSymbol is INamedTypeSymbol enumSymbol
        )
        {
            this.TypeInfo = CreateEnum(enumSymbol, typeDefinition);
        }
        else
        {
            this.TypeInfo = typeDefinition;
        }
    }

    public void AddMethod(ExportedMethodInfo methodInfo)
    {
        // ClassInfo.Methods.Add(methodInfo);
        // TypeInfo.Methods ??= [];
        TypeInfo.Methods.Add(methodInfo);
    }

    public ExportedEnumInfo CreateEnum(
        INamedTypeSymbol enumSymbol,
        ExportedTypeDefinition typeDefinition
    )
    {
        if (enumSymbol.TypeKind != TypeKind.Enum)
        {
            throw new ArgumentException("Symbol must be an enum type", nameof(enumSymbol));
        }

        var enumInfo = new ExportedEnumInfo
        {
            Options = enumSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .ToDictionary(
                    f => f.Name,
                    f =>
                        long.TryParse(f.ConstantValue?.ToString(), out var result)
                            ? result
                            : throw new ArgumentException(
                                $"Could not parse constant value for enum field '{f.Name}' in enum '{enumSymbol.Name}'."
                            )
                ),
            // copy the rest of the properties from typeDefinition
            // DefinitionId = typeDefinition.DefinitionId,
            Id = typeDefinition.Id,
            OriginalTypeWrapperName = typeDefinition.OriginalTypeWrapperName,
            SimplifiedAssemblyQualifiedName = typeDefinition.SimplifiedAssemblyQualifiedName,
            EntryPrefix = typeDefinition.EntryPrefix,
            TypeNameNoGenerics = typeDefinition.TypeNameNoGenerics,
            AssemblyQualifiedName = typeDefinition.AssemblyQualifiedName,
            FullyQualifiedName = typeDefinition.FullyQualifiedName,
            GenericTypeArgumentsToParameters = typeDefinition.GenericTypeArgumentsToParameters,
            ExportedType = typeDefinition.ExportedType,
            IsSameAsExposedType = typeDefinition.IsSameAsExposedType,
            SpecialCaseFlags = typeDefinition.SpecialCaseFlags,
            SummaryComment = typeDefinition.SummaryComment,
        };
        return enumInfo;
    }
}
