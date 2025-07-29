using System;
using System.Configuration.Assemblies;
using System.Text;
using DotWrap.Generator.Builders.Method;
using DotWrap.MSBuild;

namespace DotWrap.Generator.Builders.Class;

public class ClassMetadataBuilder
{
    public ExportedClassInfo ClassInfo { get; }

    public ClassMetadataBuilder(ClassBuilderContext classContext)
    {
        Dictionary<string, string> genericTypeParametersToArguments = new();
        for (int i = 0; i < classContext.ClassSymbol.TypeParameters.Length; i++)
        {
            var typeParam = classContext.ClassSymbol.TypeParameters[i];
            var typeArg = classContext.ClassSymbol.TypeArguments[i];
            genericTypeParametersToArguments[typeParam.Name] = typeArg.ToDisplayString();
        }
        ClassInfo = new ExportedClassInfo
        {
            Namespace = classContext.ClassSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classContext.ClassName,
            IsStatic = classContext.IsStatic,
            EntryPrefix = classContext.EntryPrefix,
            GenericTypeParametersToArguments = genericTypeParametersToArguments,
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
