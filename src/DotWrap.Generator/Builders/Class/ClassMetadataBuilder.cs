using System;
using System.Text;
using DotWrap.Generator.Builders.Method;
using DotWrap.MSBuild;

namespace DotWrap.Generator.Builders.Class;

public class ClassMetadataBuilder
{
    public ExportedClassInfo ClassInfo { get; }

    public ClassMetadataBuilder(ClassBuilderContext classContext)
    {
        ClassInfo = new ExportedClassInfo
        {
            Namespace = classContext.ClassSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classContext.ClassName,
            IsStatic = classContext.IsStatic,
            EntryPrefix = classContext.EntryPrefix,
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
