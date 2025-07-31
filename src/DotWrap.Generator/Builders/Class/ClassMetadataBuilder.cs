using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders.Class;

public class ClassMetadataBuilder
{
    public ExportedClassInfo ClassInfo { get; }

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
        ClassInfo = new ExportedClassInfo
        {
            Namespace = classContext.ClassSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classContext.ClassName,
            IsStatic = classContext.IsStatic,
            EntryPrefix = classContext.EntryPrefix,
            GenericTypeArgumentsToParameters = genericTypeArgumentsToParameters,
            Interfaces = classContext
                .ClassSymbol.AllInterfaces.Select(i => i.ToDisplayString())
                .Append(
                    classContext.ClassSymbol.TypeKind == TypeKind.Interface
                        ? classContext.ClassSymbol.ToDisplayString()
                        : null
                )
                .OfType<string>()
                .ToList(),
            SpecialCaseFlags = classContext.SpecialCaseFlags,
            SummaryComment = XmlParser.ParseSummary(
                classContext.ClassSymbol.GetDocumentationCommentXml()
            ),
        };
    }

    public void AddMethod(ExportedMethodInfo methodInfo)
    {
        ClassInfo.Methods.Add(methodInfo);
    }
}
