using System.Text;
using DotWrap.Generator.Builders.Class;
using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Method;

public class InstanceMethodBuilder(StringBuilder sb, ClassMetadataBuilder classMetadataBuilder)
{
    public void GenerateAllMethods(ClassBuilderContext classContext)
    {
        // Public instance methods
        foreach (
            var method in classContext
                .ClassSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m =>
                    m.DeclaredAccessibility == Accessibility.Public
                    && !m.IsStatic
                    && m.MethodKind == MethodKind.Ordinary
                    && !m.GetAttributes()
                        .Any(a => a.AttributeClass?.Name == nameof(DotWrapIgnoreAttribute))
                )
        )
        {
            var context = new MethodBuilderContext(method, classContext);
            var methodXml = method.GetDocumentationCommentXml();
            var exportedMethodInfo = context.GetExportedMethodInfo(methodXml);

            foreach (var param in method.Parameters)
            {
                exportedMethodInfo.Parameters.Add(
                    new ExportedParameterInfo
                    {
                        Name = param.Name,
                        Type = param.Type.ToDisplayString(),
                        Comment = XmlParser.ParseParamComment(methodXml, param.Name),
                    }
                );
            }
            classMetadataBuilder.AddMethod(exportedMethodInfo);

            switch (method.ReturnType.SpecialType)
            {
                case SpecialType.System_String:
                    GenerateSingleMethodThatReturnsString(context);
                    break;
                default:
                    GenerateSingleMethod(context);
                    break;
            }
        }
    }

    public void GenerateSingleMethod(MethodBuilderContext methodContext)
    {
        var entryPrefix = methodContext.ClassContext.EntryPrefix;
        var methodName = methodContext.MethodName;
        var parameters = GetExposedParametersString(methodContext);
        var args = string.Join(", ", methodContext.MethodSymbol.Parameters.Select(p => p.Name));
        var returnType = GetExposedReturnTypeFromOriginal(methodContext);

        var returnCall = methodContext.MethodSymbol.ReturnsVoid ? string.Empty : "return ";
        sb.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{methodName}\")]"
        );
        sb.AppendLine($"        public static {returnType} {methodName}({parameters})");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (!_instances.TryGetValue({SelfPointerName}, out var obj))");
        sb.AppendLine(
            $"                throw new System.ArgumentException(\"Invalid instance handle: {SelfPointerName}\");"
        );
        sb.AppendLine($"            {returnCall}obj.{methodName}({args});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    protected string GenerateSingleMethodThatReturnsString(MethodBuilderContext context)
    {
        var entryPrefix = context.ClassContext.EntryPrefix;
        var methodName = context.MethodName;
        var parameters = GetExposedParametersString(context);
        var args = string.Join(", ", context.MethodSymbol.Parameters.Select(p => p.Name));

        sb.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{methodName}\")]"
        );
        sb.AppendLine($"        public static IntPtr {methodName}({parameters})");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (!_instances.TryGetValue({SelfPointerName}, out var obj))");
        sb.AppendLine(
            $"                throw new System.ArgumentException(\"Invalid instance handle: {SelfPointerName}\");"
        );
        sb.AppendLine(
            $"            return global::DotWrap.BuiltIn.CString.Create(obj.{methodName}({args}));"
        );
        sb.AppendLine("        }");
        sb.AppendLine();

        return sb.ToString();
    }

    protected string GetExposedParametersString(MethodBuilderContext methodBuilderContext)
    {
        var parameters = string.Join(
            ", ",
            methodBuilderContext.MethodSymbol.Parameters.Select(p => $"{p.Type} {p.Name}")
        );
        return $"int {SelfPointerName}{(parameters.Length > 0 ? ", " : "")}{parameters}";
    }

    protected static string GetExposedReturnTypeFromOriginal(MethodBuilderContext methodContext)
    {
        return methodContext.MethodSymbol.ReturnType.SpecialType switch
        {
            // SpecialType.System_String => "global::DotWrap.System.CString",
            SpecialType.System_String => "IntPtr",
            // SpecialType.System_Void => "void",
            _ => methodContext.MethodSymbol.ReturnType.ToDisplayString(),
        };
    }
}

public record MethodBuilderContext(IMethodSymbol MethodSymbol, ClassBuilderContext ClassContext)
{
    public string MethodName => MethodSymbol.Name;

    public ExportedMethodInfo GetExportedMethodInfo()
    {
        var xmlDoc = MethodSymbol.GetDocumentationCommentXml();
        return GetExportedMethodInfo(xmlDoc);
    }

    public ExportedMethodInfo GetExportedMethodInfo(string? xmlDoc)
    {
        return new ExportedMethodInfo
        {
            Name = MethodName,
            ReturnType = MethodSymbol.ReturnType.ToDisplayString(),
            SummaryComment = XmlParser.ParseSummary(xmlDoc),
            ReturnsComment = XmlParser.ParseReturns(xmlDoc),
        };
    }
};
