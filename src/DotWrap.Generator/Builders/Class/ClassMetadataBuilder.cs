using DotWrap.Generator.Extensions;
using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders.Class;

public class ClassMetadataBuilder
{
    // public ExportedClassInfo ClassInfo { get; }
    public ExportedTypeDefinitionInfo TypeInfo { get; }

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

        TypeInfo = new ExportedTypeDefinitionInfo()
        {
            Namespace = classContext.Namespace,
            TypeName = classContext.ClassName,
            EntryPrefix = classContext.EntryPrefix,
            ExportedType = classContext.ClassSymbol.GetExportedType(out _),
            GenericTypeArgumentsToParameters = genericTypeArgumentsToParameters,
            // GenericParameters = classContext.TypeParameters.Select(tp => tp.Name).ToArray(),
            Interfaces = classContext
                .ClassSymbol.AllInterfaces.Select(i => i.ToDisplayString())
                .Append(
                    classContext.ClassSymbol.TypeKind == TypeKind.Interface
                        ? classContext.ClassSymbol.ToDisplayString()
                        : null
                )
                .OfType<string>()
                .ToList(),
            SpecialCaseFlags = classContext.ClassSymbol.GetSpecialCaseFlags(),
            SummaryComment = XmlParser.ParseSummary(
                classContext.ClassSymbol.GetDocumentationCommentXml()
            ),
        };
    }

    public void AddMethod(ExportedMethodInfo methodInfo)
    {
        // ClassInfo.Methods.Add(methodInfo);
        TypeInfo.Methods ??= [];
        TypeInfo.Methods.Add(methodInfo);
    }
}
